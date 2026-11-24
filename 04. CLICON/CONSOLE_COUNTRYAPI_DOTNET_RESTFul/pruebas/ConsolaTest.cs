using ec.edu.monster.dobles;
using ec.edu.monster.modelos;
using ec.edu.monster.servicios;
using ec.edu.monster.vista;

namespace ec.edu.monster.pruebas;

/// <summary>
/// Lo propio del cliente de consola: tablas de texto, lectura de datos campo por campo y el
/// ciclo completo inicio de sesión → menú → cierre de sesión contra un servidor falso.
/// </summary>
public class ConsolaTest
{
    private const string PERU = "{\"id\":3,\"nombre\":\"Perú\",\"capital\":\"Lima\",\"region\":\"América\","
        + "\"subregion\":\"South America\",\"poblacion\":32971846,\"area\":1285216.0,"
        + "\"moneda\":\"Peruvian sol (PEN)\"}";

    /// <summary>Consola que lee las líneas indicadas y guarda todo lo que se muestra.</summary>
    private sealed class Guion
    {
        private readonly StringWriter capturado = new();

        public Consola Consola { get; }

        public Guion(params string[] lineas)
        {
            string entrada = lineas.Length == 0 ? "" : string.Join("\n", lineas) + "\n";
            Consola = new Consola(new StringReader(entrada), capturado);
        }

        public string Salida() => capturado.ToString();
    }

    private static SesionServicio SesionActiva()
    {
        var sesion = new SesionServicio();
        sesion.Guardar("t-1", "Marcos Escobar");
        return sesion;
    }

    private static List<string> Lineas(string texto)
    {
        return texto.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToList();
    }

    public class Tablas
    {
        [Fact]
        public void AlineaNumerosALaDerechaYTextosALaIzquierda()
        {
            string texto = new Tabla("Región", "Países").Derecha(1).Fila("Asia", "52").Fila("Antártida", "5").Texto();
            List<string> lineas = Lineas(texto);
            Assert.Equal("+-----------+--------+", lineas[0]);
            Assert.Equal("| Región    | Países |", lineas[1]);
            Assert.Equal("| Asia      |     52 |", lineas[3]);
            Assert.Equal("| Antártida |      5 |", lineas[4]);
            Assert.Equal(lineas[0], lineas[5]);
        }

        [Fact]
        public void CortaLosTextosLargosConPuntosSuspensivos()
        {
            string texto = new Tabla("Moneda").Maximo(0, 8).Fila("United States dollar (USD)").Texto();
            Assert.Contains("| United … |", texto);
        }

        [Fact]
        public void SinFilasMuestraSoloElEncabezado()
        {
            Assert.Equal(3, Lineas(new Tabla("A", "B").Texto()).Count);
        }

        [Fact]
        public void FormateaNumerosConSeparadorDeMiles()
        {
            Assert.Equal("17.643.060", Tabla.Entero(17_643_060));
            Assert.Equal("276.841,5", Tabla.Decimal(276_841.5));
        }
    }

    public class Pantallas
    {
        [Fact]
        public void LosErroresLlevanElPrefijoError()
        {
            var guion = new Guion();
            guion.Consola.Error("Credenciales incorrectas");
            Assert.Equal("ERROR: Credenciales incorrectas", guion.Salida().Trim());
        }

        [Fact]
        public void FormularioDeNuevoPaisAceptaLaRegionPorNumero()
        {
            var guion = new Guion("Ecuador", "Quito", "1", "South America", "17.643.060", "276841,5", "-", "s");
            Pais pais = new PaisConsola(guion.Consola, SesionActiva()).LeerPais(null)!;
            Assert.Equal("Ecuador", pais.Nombre);
            Assert.Equal("América", pais.Region);
            Assert.Equal(17_643_060, pais.Poblacion);
            Assert.Equal(276_841.5, pais.Area);
            Assert.Null(pais.Moneda);
        }

        [Fact]
        public void EnterConservaLosValoresActuales()
        {
            var actual = new Pais("Perú", "Lima", "América", "South America", 32_971_846, 1_285_216, "Sol (PEN)") { Id = 3 };
            var guion = new Guion("", "", "europa", "", "", "", "", "si");
            Pais pais = new PaisConsola(guion.Consola, SesionActiva()).LeerPais(actual)!;
            Assert.Equal("Perú", pais.Nombre);
            Assert.Equal("Europa", pais.Region);
            Assert.Equal(32_971_846, pais.Poblacion);
            Assert.Equal(1_285_216, pais.Area);
            Assert.Contains("Datos del país", guion.Salida());
            Assert.Contains("Capital [Lima]", guion.Salida());
        }

        [Fact]
        public void ResponderNoCancelaElFormulario()
        {
            var guion = new Guion("Chile", "", "1", "", "100", "", "", "n");
            Assert.Null(new PaisConsola(guion.Consola, SesionActiva()).LeerPais(null));
        }

        [Fact]
        public void UnaRegionQueNoExisteQuedaVaciaParaQueLaValideElControlador()
        {
            Assert.Null(RegionDe("9"));
            Assert.Null(RegionDe("Atlántida"));
            Assert.Equal("Oceanía", RegionDe("5"));
        }

        private static string? RegionDe(string respuesta)
        {
            var guion = new Guion("X", "", respuesta, "", "1", "", "", "s");
            return new PaisConsola(guion.Consola, SesionActiva()).LeerPais(null)!.Region;
        }

        [Fact]
        public void ElPaisSeEligePorElIdDeLaUltimaTabla()
        {
            var guion = new Guion("7", "99", "abc");
            var vista = new PaisConsola(guion.Consola, SesionActiva());
            var peru = new Pais("Perú", "Lima", "América", null, 1, 1, null) { Id = 7 };
            vista.MostrarTabla([peru]);
            Assert.Equal("Perú", vista.PaisSeleccionado()!.Nombre);
            Assert.Null(vista.PaisSeleccionado());
            Assert.Null(vista.PaisSeleccionado());
            Assert.Contains("|  7 | Perú", guion.Salida());
        }

        [Fact]
        public void BusquedaConRegionYCriteriosVacios()
        {
            var guion = new Guion("", "lim", "8", "1");
            var vista = new BusquedaConsola(guion.Consola, SesionActiva());
            vista.MostrarFiltrosRegion(Pais.REGIONES);
            CriteriosBusqueda criterios = vista.LeerCriterios();
            Assert.Null(criterios.Nombre);
            Assert.Equal("lim", criterios.Capital);
            Assert.Equal("América", criterios.Region);
            Assert.Contains("ERROR: Opción no válida", guion.Salida());
        }

        [Fact]
        public void EscribirCeroComoUsuarioTerminaElPrograma()
        {
            var login = new LoginConsola(new Guion("0").Consola, new SesionServicio());
            Assert.Throws<Consola.Salida>(() => login.LeerCredenciales());
        }

        [Fact]
        public void SiSeAcabaLaEntradaTerminaElPrograma()
        {
            Consola consola = new Guion().Consola;
            Assert.Throws<Consola.Salida>(() => consola.Leer("Usuario"));
        }
    }

    public class Flujo
    {
        [Fact]
        public void InicioFallidoInicioCorrectoReporteYCierreDeSesion()
        {
            using var servidor = new ServidorFalso();
            // El primer intento falla y el segundo entra.
            servidor.AlPrimerPedido("POST /Login/login", 401,
                "{\"estado\":\"ERROR\",\"mensaje\":\"Credenciales incorrectas\",\"datos\":null}");
            servidor.Ok("POST /Login/login", "Ingreso de sesión exitoso", "{\"token\":\"t-9\",\"nombre\":\"Marcos Escobar\"}");
            servidor.Ok("GET /Reportes", "Reportes generados correctamente",
                "{\"paisesPorRegion\":[{\"region\":\"América\",\"cantidad\":1}],\"paisMasPoblado\":" + PERU
                + ",\"promedioPorRegion\":[{\"region\":\"América\",\"promedioPoblacion\":32971846}]}");
            servidor.Ok("POST /Login/logout", "Sesión cerrada", "null");
            var guion = new Guion("monster", "mala", "monster", "monster9", "9", "5", "7", "0");

            new Program(guion.Consola, new ClienteRest(servidor.UrlBase(), 5)).Ejecutar();

            string salida = guion.Salida();
            Assert.Contains("ERROR: Credenciales incorrectas", salida);
            Assert.Contains("Bienvenido, Marcos Escobar.", salida);
            Assert.Contains("ERROR: Opción no válida", salida);
            Assert.Contains("Perú · 32.971.846 habitantes · América", salida);
            Assert.Contains("Servidor disponible", salida);
            Assert.EndsWith("Hasta luego.", salida.Trim());
            Assert.Equal("t-9", servidor.Ultima()!.Authorization!.Replace("Bearer ", ""));
            // El encabezado del inicio de sesión: al entrar y después de cerrar sesión.
            Assert.Equal(2, Contar(salida, "CountryAPI · Iniciar sesión"));
        }

        [Fact]
        public void SiLaSesionExpiraVuelveAlInicioDeSesion()
        {
            using var servidor = new ServidorFalso();
            servidor.Ok("POST /Login/login", "Ingreso de sesión exitoso", "{\"token\":\"t-9\",\"nombre\":\"Marcos Escobar\"}");
            servidor.Error("GET /Paises", 401, "Token inválido o sesión expirada");
            var guion = new Guion("monster", "monster9", "3", "0");
            new Program(guion.Consola, new ClienteRest(servidor.UrlBase(), 5)).Ejecutar();

            string salida = guion.Salida();
            Assert.Contains("ERROR: " + ClienteRest.SESION_EXPIRADA, salida);
            Assert.Equal(2, Contar(salida, "CountryAPI · Iniciar sesión"));
            Assert.DoesNotContain("1. Listar todos", salida);
        }

        private static int Contar(string texto, string buscado)
        {
            int veces = 0;
            for (int i = texto.IndexOf(buscado, StringComparison.Ordinal); i >= 0;
                 i = texto.IndexOf(buscado, i + 1, StringComparison.Ordinal))
            {
                veces++;
            }
            return veces;
        }
    }
}
