using ec.edu.monster.controlador;
using ec.edu.monster.dobles;
using ec.edu.monster.modelos;
using ec.edu.monster.servicios;

namespace ec.edu.monster.pruebas;

/// <summary>
/// Controladores del cliente con una vista en memoria y un servidor RESTful falso.
/// </summary>
public class ControladoresTest
{
    private const string PERU = "{\"id\":3,\"nombre\":\"Perú\",\"capital\":\"Lima\",\"region\":\"América\","
        + "\"subregion\":\"South America\",\"poblacion\":32971846,\"area\":1285216.0,"
        + "\"moneda\":\"Peruvian sol (PEN)\",\"guardado\":true}";

    /// <summary>Lo que comparten todas las pruebas: servidor falso, cliente, sesión y vista.</summary>
    public abstract class Base : IDisposable
    {
        protected readonly ServidorFalso servidor = new();
        protected readonly ClienteRest cliente;
        protected readonly SesionServicio sesion = new();
        protected readonly VistaFalsa vista = new();

        protected Base()
        {
            cliente = new ClienteRest(servidor.UrlBase(), 5);
            sesion.Guardar("t-1", "Marcos Escobar");
        }

        public void Dispose()
        {
            servidor.Dispose();
            GC.SuppressFinalize(this);
        }

        protected static Pais NuevoPais(string nombre, string region, long poblacion, double area)
        {
            return new Pais(nombre, "Capital", region, "Sub", poblacion, area, "Moneda");
        }
    }

    public class Login : Base
    {
        private readonly LoginControlador controlador;

        public Login()
        {
            sesion.Cerrar();
            controlador = new LoginControlador(vista, vista, new LoginServicio(cliente), sesion);
        }

        [Fact]
        public async Task CamposVaciosNoLlamanAlServidor()
        {
            vista.Usuario = " ";
            vista.Clave = "x";
            await controlador.IniciarSesion();
            Assert.Equal("Ingrese el usuario", vista.UltimoError());
            Assert.Empty(servidor.Peticiones());
        }

        [Fact]
        public async Task CredencialesIncorrectasBorranLaClave()
        {
            servidor.Error("POST /Login/login", 401, "Credenciales incorrectas");
            vista.Usuario = "monster";
            vista.Clave = "mala";
            await controlador.IniciarSesion();
            Assert.Equal("Credenciales incorrectas", vista.UltimoError());
            Assert.True(vista.ClaveLimpiada);
            Assert.Null(vista.MenuMostradoPara);
            Assert.False(sesion.EstaActiva());
        }

        [Fact]
        public async Task SinConexionConservaLosDatos()
        {
            var sinServidor = new LoginControlador(vista, vista,
                new LoginServicio(new ClienteRest("http://127.0.0.1:9/api", 2)), sesion);
            vista.Usuario = "monster";
            vista.Clave = "monster9";
            await sinServidor.IniciarSesion();
            Assert.Equal(ClienteRest.SIN_CONEXION, vista.UltimoError());
            Assert.False(vista.ClaveLimpiada);
        }

        [Fact]
        public async Task CredencialesCorrectasGuardanLaSesionYMuestranElMenu()
        {
            servidor.Ok("POST /Login/login", "Ingreso de sesión exitoso", "{\"token\":\"t-9\",\"nombre\":\"Marcos Escobar\"}");
            vista.Usuario = " monster ";
            vista.Clave = "monster9";
            await controlador.IniciarSesion();
            Assert.Equal("t-9", sesion.ObtenerToken());
            Assert.Equal("Marcos Escobar", vista.MenuMostradoPara);
            // Se envía sin espacios.
            Assert.Contains("\"usuario\":\"monster\"", servidor.Ultima()!.Cuerpo);
        }

        [Fact]
        public async Task CerrarSesionAvisaAlServidorYVuelveAlLogin()
        {
            servidor.Ok("POST /Login/logout", "Sesión cerrada", "null");
            sesion.Guardar("t-9", "Marcos Escobar");
            await controlador.CerrarSesion();
            Assert.Equal("Bearer t-9", servidor.Ultima()!.Authorization);
            Assert.False(sesion.EstaActiva());
            Assert.Equal(1, vista.VecesInicioSesion);
            Assert.Equal(1, vista.VecesFormularioLogin);
        }

        [Fact]
        public async Task CerrarSesionFuncionaAunqueElServidorNoResponda()
        {
            var sinServidor = new LoginControlador(vista, vista,
                new LoginServicio(new ClienteRest("http://127.0.0.1:9/api", 2)), sesion);
            sesion.Guardar("t-9", "Marcos Escobar");
            await sinServidor.CerrarSesion();
            Assert.False(sesion.EstaActiva());
            Assert.Equal(1, vista.VecesInicioSesion);
        }
    }

    public class Paises : Base
    {
        private readonly PaisControlador controlador;

        public Paises()
        {
            controlador = new PaisControlador(vista, vista, new PaisServicio(cliente), sesion);
            servidor.Ok("GET /Paises", "Consulta realizada correctamente", "[" + PERU + "]");
        }

        [Fact]
        public void MuestraLosFiltrosDeRegionAlCrearse()
        {
            Assert.Equal(Pais.REGIONES, vista.FiltrosRegion);
        }

        [Fact]
        public async Task ListarMuestraLaTabla()
        {
            await controlador.Listar();
            Assert.Equal("Perú", vista.Tabla![0].Nombre);
            Assert.Equal("Bearer t-1", servidor.Ultima()!.Authorization);
        }

        [Fact]
        public async Task TablaVaciaSugiereImportar()
        {
            servidor.Ok("GET /Paises", "Consulta realizada correctamente", "[]");
            await controlador.Listar();
            Assert.Equal("No hay países registrados. Use «Importar países» para cargarlos", vista.UltimoMensaje());
        }

        [Fact]
        public async Task SesionExpiradaVuelveAlInicioDeSesion()
        {
            servidor.Error("GET /Paises", 401, "Token inválido o sesión expirada");
            await controlador.Listar();
            Assert.Equal(ClienteRest.SESION_EXPIRADA, vista.UltimoError());
            Assert.False(sesion.EstaActiva());
            Assert.Equal(1, vista.VecesInicioSesion);
        }

        [Fact]
        public async Task BuscarEnviaElTextoYAvisaSiNoHayResultados()
        {
            servidor.Ok("GET /Paises", "Consulta realizada correctamente", "[]");
            vista.Filtro = "zz";
            await controlador.Buscar();
            Assert.Equal("/Paises?nombre=zz", servidor.Ultima()!.Ruta);
            Assert.Equal("No se encontraron países", vista.UltimoMensaje());
        }

        [Fact]
        public async Task RegistrarReabreElFormularioSiLosDatosSonInvalidos()
        {
            servidor.Responder("POST /Paises", 201,
                "{\"estado\":\"OK\",\"mensaje\":\"País registrado correctamente\",\"datos\":{\"id\":20}}");
            Pais invalido = NuevoPais("Chile", "América", -1, 10);
            Pais valido = NuevoPais("Chile", "América", 19116209, 756102);
            vista.RespuestasFormulario.Enqueue(invalido);
            vista.RespuestasFormulario.Enqueue(valido);
            await controlador.Registrar();
            Assert.Equal("La población debe ser un número entero mayor o igual que cero", vista.Errores[0]);
            // El primero se abre vacío; el segundo conserva lo escrito.
            Assert.Null(vista.FormulariosAbiertos[0]);
            Assert.Same(invalido, vista.FormulariosAbiertos[1]);
            Assert.Single(servidor.Peticiones(), p => p.Metodo == "POST");
            Assert.Equal("País registrado correctamente", vista.UltimoMensaje());
            // La tabla se recarga.
            Assert.Equal("Perú", vista.Tabla![0].Nombre);
        }

        [Fact]
        public async Task RegistrarUnNombreRepetidoMuestraElErrorYPermiteCorregir()
        {
            servidor.Error("POST /Paises", 409, "Ya existe un país con ese nombre");
            vista.RespuestasFormulario.Enqueue(NuevoPais("Perú", "América", 1, 1));
            vista.RespuestasFormulario.Enqueue(null);
            await controlador.Registrar();
            Assert.Equal("Ya existe un país con ese nombre", vista.UltimoError());
            // Se reabre y el usuario cancela.
            Assert.Equal(2, vista.FormulariosAbiertos.Count);
        }

        [Fact]
        public async Task ActualizarEnviaPutConElIdDelSeleccionado()
        {
            servidor.Ok("PUT /Paises/3", "País actualizado correctamente", PERU);
            Pais seleccionado = NuevoPais("Perú", "América", 1, 1);
            seleccionado.Id = 3;
            vista.Seleccionado = seleccionado;
            vista.RespuestasFormulario.Enqueue(NuevoPais("Perú", "América", 33000000, 1285216));
            await controlador.Actualizar();
            Assert.Equal("PUT", servidor.Peticiones()[0].Metodo);
            Assert.Equal("País actualizado correctamente", vista.UltimoMensaje());
            Assert.Equal(3, vista.FormulariosAbiertos[0]!.Id);
        }

        [Fact]
        public async Task EditarOEliminarSinSeleccionPideElegirUnPais()
        {
            await controlador.Actualizar();
            await controlador.Eliminar();
            Assert.Equal(2, vista.Errores.Count(e => e == "Seleccione un país de la tabla"));
            Assert.Empty(servidor.Peticiones());
        }

        [Fact]
        public async Task EliminarSinConfirmarNoEnviaNada()
        {
            Pais seleccionado = NuevoPais("Perú", "América", 1, 1);
            seleccionado.Id = 3;
            vista.Seleccionado = seleccionado;
            vista.Confirmar = false;
            await controlador.Eliminar();
            Assert.Same(seleccionado, vista.ConfirmacionPedidaPara);
            Assert.Empty(servidor.Peticiones());
        }

        [Fact]
        public async Task EliminarConfirmadoEnviaDeleteYRecarga()
        {
            servidor.Ok("DELETE /Paises/3", "País eliminado correctamente", "null");
            Pais seleccionado = NuevoPais("Perú", "América", 1, 1);
            seleccionado.Id = 3;
            vista.Seleccionado = seleccionado;
            await controlador.Eliminar();
            Assert.Equal("DELETE", servidor.Peticiones()[0].Metodo);
            Assert.Equal("País eliminado correctamente", vista.UltimoMensaje());
        }

        [Fact]
        public async Task BuscarYFiltrarMuestraElTotalOAvisaSinResultados()
        {
            servidor.Ok("GET /Paises/buscar", "Búsqueda realizada correctamente", "[" + PERU + "]");
            vista.Criterios = new CriteriosBusqueda(null, "lima", "América");
            await controlador.BuscarYFiltrar();
            Assert.Equal(1, vista.TotalBusqueda);
            Assert.Equal("/Paises/buscar?capital=lima&region=Am%C3%A9rica", servidor.Ultima()!.Ruta);
            servidor.Ok("GET /Paises/buscar", "Búsqueda realizada correctamente", "[]");
            await controlador.BuscarYFiltrar();
            Assert.Equal("No se encontraron países con esos criterios", vista.UltimoMensaje());
        }
    }

    public class Api : Base
    {
        private readonly ApiControlador controlador;

        public Api()
        {
            controlador = new ApiControlador(vista, vista, new PaisServicio(cliente), sesion);
        }

        [Fact]
        public async Task ImportarCanceladoNoEnviaNada()
        {
            vista.ConfirmarImportacionUsuario = false;
            await controlador.Importar();
            Assert.Empty(servidor.Peticiones());
        }

        [Fact]
        public async Task ImportarMuestraElResumen()
        {
            servidor.Ok("POST /Paises/importar", "Importación finalizada",
                "{\"recibidos\":254,\"registrados\":242,\"actualizados\":12}");
            await controlador.Importar();
            Assert.Equal(254, vista.Resumen!.Recibidos);
            Assert.Equal(12, vista.Resumen.Actualizados);
        }

        [Fact]
        public async Task ConsultarExigeDosCaracteres()
        {
            vista.NombreConsulta = "e";
            await controlador.Consultar();
            Assert.Equal("Ingrese al menos dos caracteres", vista.UltimoError());
            Assert.Empty(servidor.Peticiones());
        }

        [Fact]
        public async Task ConsultarSinCoincidenciasMuestraElMensajeDelServidor()
        {
            servidor.Error("GET /Paises/externo/zzz", 404, "No se encontraron países con ese nombre en la API");
            vista.NombreConsulta = "zzz";
            await controlador.Consultar();
            Assert.Equal("No se encontraron países con ese nombre en la API", vista.UltimoError());
        }

        [Fact]
        public async Task GuardarMarcaElPaisComoGuardado()
        {
            servidor.Ok("GET /Paises/externo/chile", "Consulta en la API exitosa",
                "[{\"nombre\":\"Chile\",\"region\":\"América\",\"poblacion\":1,\"area\":1,\"guardado\":false}]");
            servidor.Ok("POST /Paises/guardar", "País guardado correctamente", "{\"id\":40,\"nombre\":\"Chile\"}");
            vista.NombreConsulta = "chile";
            await controlador.Consultar();
            Pais chile = vista.ResultadosConsulta![0];
            Assert.False(chile.Guardado);
            vista.SeleccionConsulta = chile;
            await controlador.Guardar();
            Assert.True(vista.ResultadosConsulta[0].Guardado);
            Assert.Equal("País guardado correctamente", vista.UltimoMensaje());
        }

        [Fact]
        public async Task GuardarSinSeleccionPideElegirUnPais()
        {
            await controlador.Guardar();
            Assert.Equal("Seleccione un país de la tabla", vista.UltimoError());
        }
    }

    public class Reportes : Base
    {
        [Fact]
        public async Task SinPaisesSugiereImportar()
        {
            servidor.Error("GET /Reportes", 404, "No hay países registrados para generar reportes");
            await new ReporteControlador(vista, new ReporteServicio(cliente), sesion).Generar();
            Assert.Equal("No hay países registrados para generar reportes. Use «Importar países» para cargarlos.",
                vista.UltimoError());
        }

        [Fact]
        public async Task MuestraElReporte()
        {
            servidor.Ok("GET /Reportes", "Reportes generados correctamente",
                "{\"paisesPorRegion\":[],\"paisMasPoblado\":" + PERU + ",\"promedioPorRegion\":[]}");
            await new ReporteControlador(vista, new ReporteServicio(cliente), sesion).Generar();
            Assert.Equal("Perú", vista.Reporte!.PaisMasPoblado!.Nombre);
        }
    }

    public class Universidades : Base
    {
        private readonly UniversidadControlador controlador;

        public Universidades()
        {
            controlador = new UniversidadControlador(vista, new UniversidadServicio(cliente), sesion);
        }

        [Fact]
        public async Task PaisVacioNoLlamaAlServidor()
        {
            vista.PaisUniversidades = " ";
            await controlador.Importar();
            Assert.Equal("Ingrese el país", vista.UltimoError());
            Assert.Empty(servidor.Peticiones());
        }

        [Fact]
        public async Task ImportarMuestraElResumenYRecargaLaTabla()
        {
            servidor.Ok("POST /Universidades/importar", "Importación de universidades finalizada",
                "{\"recibidos\":43,\"registrados\":38,\"actualizados\":5}");
            servidor.Ok("GET /Universidades", "Consulta realizada correctamente",
                "[{\"nombre\":\"Escuela Politécnica del Ejército\",\"dominio\":\"espe.edu.ec\"}]");
            vista.PaisUniversidades = "Ecuador";
            await controlador.Importar();
            Assert.Equal(38, vista.Resumen!.Registrados);
            Assert.Equal("espe.edu.ec", vista.Universidades![0].Dominio);
            Assert.Equal("/Universidades?pais=Ecuador", servidor.Ultima()!.Ruta);
        }

        [Fact]
        public async Task ListarVacioAvisa()
        {
            servidor.Ok("GET /Universidades", "Consulta realizada correctamente", "[]");
            vista.PaisUniversidades = "Narnia";
            await controlador.Listar();
            Assert.Equal("No hay universidades guardadas para ese país", vista.UltimoMensaje());
        }
    }
}
