using ec.edu.monster.dobles;
using ec.edu.monster.servicios;

namespace ec.edu.monster.pruebas;

/// <summary>
/// El cliente web completo (filtros, controladores MVC y vistas Razor) con un navegador en
/// memoria y el servidor RESTful falso: inicio de sesión, CSRF, control de acceso, CRUD de
/// países con el formulario que se reabre, consulta y guardado sin repetir la consulta, y
/// sesión expirada.
/// </summary>
public class WebTest
{
    private const string PERU = "{\"id\":3,\"nombre\":\"Perú\",\"capital\":\"Lima\",\"region\":\"América\","
        + "\"subregion\":\"South America\",\"poblacion\":32971846,\"area\":1285216.0,"
        + "\"moneda\":\"Peruvian sol (PEN)\"}";

    /// <summary>Servidor falso y navegador nuevos para cada prueba.</summary>
    public abstract class Base : IDisposable
    {
        protected readonly ServidorFalso servidor = new();
        protected readonly Navegador navegador;

        protected Base()
        {
            navegador = new Navegador(servidor.UrlBase());
        }

        public void Dispose()
        {
            navegador.Dispose();
            servidor.Dispose();
            GC.SuppressFinalize(this);
        }

        protected async Task IniciarSesion()
        {
            servidor.Ok("POST /Login/login", "Ingreso de sesión exitoso", "{\"token\":\"t-9\",\"nombre\":\"Marcos Escobar\"}");
            Navegador.Resultado r = await navegador.Post("/login", "usuario", "monster", "clave", "monster9");
            Assert.Equal("/menu", r.Redireccion);
        }

        protected int Peticiones(string metodo, string rutaInicio)
        {
            return servidor.Peticiones().Count(p => p.Metodo == metodo && p.Ruta.StartsWith(rutaInicio, StringComparison.Ordinal));
        }
    }

    public class InicioDeSesion : Base
    {
        [Fact]
        public async Task CredencialesCorrectasCambianElIdYLlevanAlMenuConSaludo()
        {
            string idAntes = Assert.Single((await navegador.Get("/login")).CookiesSesion);
            servidor.Ok("POST /Login/login", "Ingreso de sesión exitoso", "{\"token\":\"t-9\",\"nombre\":\"Marcos Escobar\"}");

            Navegador.Resultado r = await navegador.Post("/login", "usuario", " monster ", "clave", "monster9");

            Assert.Equal("/menu", r.Redireccion);
            string idDespues = Assert.Single(r.CookiesSesion);
            Assert.NotEqual(idAntes, idDespues);
            Assert.Contains("\"usuario\":\"monster\"", servidor.Ultima()!.Cuerpo);
            Navegador.Resultado menu = await navegador.Get("/menu");
            Assert.Contains("Bienvenido, Marcos Escobar.", menu.Html);
            Assert.Contains("<span class=\"barra__nombre\">Marcos Escobar</span>", menu.Html);
        }

        [Fact]
        public async Task CredencialesIncorrectasVuelvenAlFormularioConElError()
        {
            servidor.Error("POST /Login/login", 401, "Credenciales incorrectas");
            Navegador.Resultado r = await navegador.Post("/login", "usuario", "monster", "clave", "mala");
            Assert.Equal(200, r.Codigo);
            Assert.Null(r.Redireccion);
            Assert.Contains("Credenciales incorrectas", r.Html);
            Assert.Contains("value=\"monster\"", r.Html);
        }

        [Fact]
        public async Task SinUsuarioMuestraElMensajeDelEcudSinLlamarAlServidor()
        {
            Navegador.Resultado r = await navegador.Post("/login", "usuario", "", "clave", "x");
            Assert.Contains("Ingrese el usuario", r.Html);
            Assert.Empty(servidor.Peticiones());
        }

        [Fact]
        public async Task ConSesionAbiertaElLoginLlevaAlMenu()
        {
            await IniciarSesion();
            Assert.Equal("/menu", (await navegador.Get("/login")).Redireccion);
        }

        [Fact]
        public async Task CerrarSesionAvisaAlServidorEInvalidaLaSesion()
        {
            await IniciarSesion();
            servidor.Ok("POST /Login/logout", "Sesión cerrada", "null");
            Navegador.Resultado r = await navegador.Post("/logout");
            Assert.Equal("/login", r.Redireccion);
            Assert.Equal("POST", servidor.Ultima()!.Metodo);
            Assert.Equal("/Login/logout", servidor.Ultima()!.Ruta);
            Assert.Equal("/login", (await navegador.Get("/menu")).Redireccion);
        }
    }

    public class Filtros : Base
    {
        [Fact]
        public async Task SinSesionLasPantallasProtegidasLlevanAlLogin()
        {
            Navegador.Resultado r = await navegador.Get("/paises");
            Assert.Equal("/login", r.Redireccion);
            Assert.Empty(servidor.Peticiones());
        }

        [Fact]
        public async Task ConSesionPasaAlControlador()
        {
            await IniciarSesion();
            servidor.Ok("GET /Paises", "Consulta realizada correctamente", "[]");
            Navegador.Resultado r = await navegador.Get("/paises");
            Assert.Equal(200, r.Codigo);
            Assert.Contains("<h1 class=\"titulo\">Países</h1>", r.Html);
        }

        [Fact]
        public async Task UnPostSinTokenCsrfSeRechaza()
        {
            await IniciarSesion();
            Navegador.Resultado r = await navegador.PostSinToken("/paises", "accion", "eliminar", "id", "3", "confirmado", "si");
            Assert.Equal(403, r.Codigo);
            Assert.Contains("La solicitud no es válida o la página caducó. Vuelva a intentarlo desde el menú.", r.Html);
            Assert.Equal(0, Peticiones("DELETE", "/Paises"));
        }

        [Fact]
        public async Task UnPostConElTokenDeLaPaginaPasa()
        {
            Navegador.Resultado get = await navegador.Get("/login");
            Assert.Contains("name=\"_csrf\"", get.Html);
            Navegador.Resultado post = await navegador.Post("/login", "usuario", "", "clave", "");
            Assert.Equal(200, post.Codigo);
            Assert.Contains("Ingrese el usuario", post.Html);
        }

        [Fact]
        public async Task UnLoginConTokenVencidoMuestraUnFormularioNuevo()
        {
            await navegador.Get("/login");
            Navegador.Resultado r = await navegador.PostSinToken("/login", "_csrf", "otro", "usuario", "monster", "clave", "monster9");
            Assert.Equal("/login", r.Redireccion);
            Assert.Empty(servidor.Peticiones());
        }
    }

    public class Paises : Base
    {
        private async Task ConSesion()
        {
            await IniciarSesion();
            servidor.Ok("GET /Paises", "Consulta realizada correctamente", "[" + PERU + "]");
        }

        [Fact]
        public async Task ListaLosPaisesYAbreElFormularioDeEdicion()
        {
            await ConSesion();
            Navegador.Resultado r = await navegador.Get("/paises?editar=3");
            Assert.Equal(200, r.Codigo);
            Assert.Contains("1 país", r.Html);
            Assert.Contains("Datos del país", r.Html);
            Assert.Contains("id=\"f-capital\" name=\"capital\" value=\"Lima\"", r.Html);
            Assert.Contains("<option value=\"América\" selected=\"selected\">América</option>", r.Html);
            Assert.Contains("<option value=\"Antártida\">Antártida</option>", r.Html);
        }

        [Fact]
        public async Task DatosInvalidosReabrenElFormularioConLoEscrito()
        {
            await ConSesion();
            await navegador.Get("/paises");
            Navegador.Resultado r = await navegador.Post("/paises",
                "accion", "registrar", "nombre", "Chile", "region", "América", "poblacion", "abc");
            Assert.Equal(200, r.Codigo);
            Assert.Contains("La población debe ser un número entero mayor o igual que cero", r.Html);
            Assert.Contains("id=\"f-nombre\" name=\"nombre\" value=\"Chile\"", r.Html);
            Assert.Equal(0, Peticiones("POST", "/Paises"));
        }

        [Fact]
        public async Task UnDuplicadoDelServidorTambienReabreElFormulario()
        {
            await ConSesion();
            servidor.Error("POST /Paises", 409, "Ya existe un país con ese nombre");
            Navegador.Resultado r = await navegador.Post("/paises",
                "accion", "registrar", "nombre", "Perú", "region", "América", "poblacion", "1");
            Assert.Contains("Ya existe un país con ese nombre", r.Html);
            Assert.Contains("Nuevo país", r.Html);
        }

        [Fact]
        public async Task RegistrarBienRedirigeALaListaConElMensaje()
        {
            await ConSesion();
            servidor.Responder("POST /Paises", 201,
                "{\"estado\":\"OK\",\"mensaje\":\"País registrado correctamente\",\"datos\":" + PERU + "}");
            Navegador.Resultado r = await navegador.Post("/paises",
                "accion", "registrar", "nombre", "Perú", "region", "América", "poblacion", "32.971.846", "area", "1285216,5");
            Assert.Equal("/paises", r.Redireccion);
            string cuerpo = servidor.Peticiones().First(p => p.Metodo == "POST" && p.Ruta == "/Paises").Cuerpo;
            Assert.Contains("\"poblacion\":32971846", cuerpo);
            Assert.Contains("1285216.5", cuerpo);
            Assert.Contains("País registrado correctamente", (await navegador.Get("/paises")).Html);
        }

        [Fact]
        public async Task EliminarSinConfirmarNoLlamaAlServidor()
        {
            await ConSesion();
            await navegador.Get("/paises");
            await navegador.Post("/paises", "accion", "eliminar", "id", "3");
            Assert.Equal(0, Peticiones("DELETE", "/Paises"));
        }

        [Fact]
        public async Task EliminarConfirmadoUsaElIdDeLaTabla()
        {
            await ConSesion();
            servidor.Ok("DELETE /Paises/3", "País eliminado correctamente", "null");
            Navegador.Resultado confirmacion = await navegador.Get("/paises?eliminar=3");
            Assert.Contains("¿Eliminar el país Perú?", confirmacion.Html);
            Navegador.Resultado r = await navegador.Post("/paises", "accion", "eliminar", "id", "3", "confirmado", "si");
            Assert.Equal("/paises", r.Redireccion);
            Assert.Contains("País eliminado correctamente", (await navegador.Get("/paises")).Html);
        }

        [Fact]
        public async Task SiElServidorRechazaElTokenVuelveAlLoginConElAviso()
        {
            await IniciarSesion();
            servidor.Error("GET /Paises", 401, "Token inválido o sesión expirada");
            Navegador.Resultado r = await navegador.Get("/paises");
            Assert.Equal("/login", r.Redireccion);
            Assert.Contains(ClienteRest.SESION_EXPIRADA, (await navegador.Get("/login")).Html);
            Assert.Equal("/login", (await navegador.Get("/menu")).Redireccion);
        }
    }

    public class ConsultaYUniversidades : Base
    {
        [Fact]
        public async Task GuardarUsaLaFilaElegidaSinVolverAConsultarLaApi()
        {
            await IniciarSesion();
            servidor.Ok("GET /Paises/externo/peru", "Consulta en la API exitosa", "[" + PERU + "]");
            servidor.Ok("POST /Paises/guardar", "País guardado correctamente", PERU);
            Navegador.Resultado consulta = await navegador.Get("/consultar?nombre=peru");
            Assert.Contains("1 resultado", consulta.Html);

            Navegador.Resultado guardar = await navegador.Post("/consultar", "numero", "1");
            Assert.Equal("/consultar", guardar.Redireccion);
            Assert.Contains("\"nombre\":\"Perú\"", servidor.Ultima()!.Cuerpo);

            Navegador.Resultado despues = await navegador.Get("/consultar");
            Assert.Contains("<span class=\"pastilla pastilla--si\">Sí</span>", despues.Html);
            Assert.Contains("País guardado correctamente", despues.Html);
            Assert.Equal(1, Peticiones("GET", "/Paises/externo"));
        }

        [Fact]
        public async Task VerGuardadasSoloCambiaDePais()
        {
            await IniciarSesion();
            Navegador.Resultado r = await navegador.Post("/universidades", "accion", "listar", "pais", "United States");
            Assert.Equal("/universidades?pais=United%20States", r.Redireccion);
            Assert.Equal(0, Peticiones("GET", "/Universidades") + Peticiones("POST", "/Universidades"));
        }

        [Fact]
        public async Task SinPaisSeMuestranLasDeEcuador()
        {
            await IniciarSesion();
            servidor.Ok("GET /Universidades", "Consulta realizada correctamente", "[]");
            Navegador.Resultado r = await navegador.Get("/universidades");
            Assert.Contains("id=\"pais\" name=\"pais\" value=\"Ecuador\"", r.Html);
            Assert.Equal("/Universidades?pais=Ecuador", servidor.Ultima()!.Ruta);
        }
    }
}
