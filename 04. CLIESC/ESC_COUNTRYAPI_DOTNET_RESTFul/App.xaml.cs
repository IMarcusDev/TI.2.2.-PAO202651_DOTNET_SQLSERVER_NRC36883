using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using ec.edu.monster.controlador;
using ec.edu.monster.servicios;
using ec.edu.monster.vista;

namespace ec.edu.monster;

/// <summary>
/// Punto de entrada del cliente de escritorio. Crea el modelo (servicios y sesión), las vistas
/// WPF y los controladores, y los conecta (vista → controlador → servicios → ClienteRest).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Un error no previsto se registra y no cierra la aplicación.
        DispatcherUnhandledException += AlErrorNoPrevisto;
        VentanaPrincipal ventana = Construir(new ClienteRest());
        MainWindow = ventana;
        ventana.Show();
    }

    private static void AlErrorNoPrevisto(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Trace.TraceError("Error inesperado: " + e.Exception);
        e.Handled = true;
    }

    /// <summary>Arma la ventana completa contra el servidor del cliente indicado.</summary>
    public static VentanaPrincipal Construir(ClienteRest cliente)
    {
        var sesion = new SesionServicio();
        var ventana = new VentanaPrincipal(cliente.UrlBase, cliente.ServidorDisponible);

        // Inicio y cierre de sesión
        var login = new LoginPanel(ventana);
        ventana.EstablecerLogin(login);
        var loginControlador = new LoginControlador(login, ventana, new LoginServicio(cliente), sesion);
        login.EstablecerControlador(loginControlador);

        // Países: CRUD, búsqueda, importación y consulta en la API
        var paisServicio = new PaisServicio(cliente);
        var importacion = new ImportacionPanel(ventana);
        var consulta = new ConsultaApiPanel(ventana);
        var apiControlador = new ApiControlador(consulta, importacion, paisServicio, sesion);
        importacion.EstablecerControlador(apiControlador);
        consulta.EstablecerControlador(apiControlador);
        var paises = new PaisPanel(ventana);
        var busqueda = new BusquedaPanel(ventana);
        var paisControlador = new PaisControlador(paises, busqueda, paisServicio, sesion);
        paises.EstablecerControlador(paisControlador);
        busqueda.EstablecerControlador(paisControlador);

        // Reportes y universidades
        var reportes = new ReportePanel(ventana);
        var reporteControlador = new ReporteControlador(reportes, new ReporteServicio(cliente), sesion);
        reportes.EstablecerControlador(reporteControlador);
        var universidades = new UniversidadPanel(ventana);
        var universidadControlador = new UniversidadControlador(universidades, new UniversidadServicio(cliente), sesion);
        universidades.EstablecerControlador(universidadControlador);

        // Opciones del menú, en el orden del ECUD1
        ventana.AgregarPantalla("importar", "Importar países",
            "Trae todos los países desde la API REST Countries.", importacion, null);
        ventana.AgregarPantalla("consultar", "Consultar país en la API",
            "Busca un país por nombre en la API y guárdelo.", consulta, null);
        ventana.AgregarPantalla("paises", "Países",
            "Lista, registra, edita y elimina los países guardados.", paises, paisControlador.Listar);
        ventana.AgregarPantalla("buscar", "Buscar y filtrar",
            "Busca por nombre, capital y región con filtros dinámicos.", busqueda, paisControlador.BuscarYFiltrar);
        ventana.AgregarPantalla("reportes", "Reportes",
            "Países por región, país más poblado y promedio poblacional.", reportes, reporteControlador.Generar);
        ventana.AgregarPantalla("universidades", "Universidades",
            "Importa y consulta las universidades de un país.", universidades, universidadControlador.Listar);
        ventana.EstablecerAccionCerrarSesion(loginControlador.CerrarSesion);
        return ventana;
    }
}
