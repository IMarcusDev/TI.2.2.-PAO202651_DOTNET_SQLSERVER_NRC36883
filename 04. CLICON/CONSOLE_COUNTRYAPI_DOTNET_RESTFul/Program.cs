using ec.edu.monster.controlador;
using ec.edu.monster.servicios;
using ec.edu.monster.vista;

namespace ec.edu.monster;

/// <summary>
/// Punto de entrada del cliente de consola (el App de Java). Crea el modelo (servicios y
/// sesión), las pantallas de texto y los controladores, los conecta
/// (vista → controlador → servicios → ClienteRest) y recorre el ciclo inicio de sesión → menú
/// principal hasta que el usuario sale.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public sealed class Program
{
    private readonly Consola consola;
    private readonly SesionServicio sesion = new();
    private readonly LoginConsola login;
    private readonly MenuConsola menu;
    private readonly LoginControlador loginControlador;
    private readonly ImportacionConsola importacion;
    private readonly ConsultaApiConsola consulta;
    private readonly PaisConsola paises;
    private readonly BusquedaConsola busqueda;
    private readonly ReporteConsola reportes;
    private readonly UniversidadConsola universidades;

    public Program(Consola consola, ClienteRest cliente)
    {
        this.consola = consola;

        // Inicio y cierre de sesión
        login = new LoginConsola(consola, sesion);
        menu = new MenuConsola(consola, sesion, cliente.UrlBase, () => cliente.ServidorDisponible().GetAwaiter().GetResult());
        loginControlador = new LoginControlador(login, menu, new LoginServicio(cliente), sesion);

        // Países: CRUD, búsqueda, importación y consulta en la API
        var paisServicio = new PaisServicio(cliente);
        importacion = new ImportacionConsola(consola, sesion);
        consulta = new ConsultaApiConsola(consola, sesion);
        var apiControlador = new ApiControlador(consulta, importacion, paisServicio, sesion);
        importacion.EstablecerControlador(apiControlador);
        consulta.EstablecerControlador(apiControlador);

        paises = new PaisConsola(consola, sesion);
        busqueda = new BusquedaConsola(consola, sesion);
        var paisControlador = new PaisControlador(paises, busqueda, paisServicio, sesion);
        paises.EstablecerControlador(paisControlador);
        busqueda.EstablecerControlador(paisControlador);

        // Reportes y universidades
        reportes = new ReporteConsola(consola, sesion);
        reportes.EstablecerControlador(new ReporteControlador(reportes, new ReporteServicio(cliente), sesion));
        universidades = new UniversidadConsola(consola, sesion);
        universidades.EstablecerControlador(new UniversidadControlador(universidades, new UniversidadServicio(cliente), sesion));
    }

    public static void Main()
    {
        new Program(Consola.DelSistema(), new ClienteRest()).Ejecutar();
    }

    /// <summary>Inicio de sesión y menú hasta que el usuario escribe 0 como usuario o se acaba la entrada.</summary>
    public void Ejecutar()
    {
        try
        {
            bool mostrarFormulario = true;
            while (true)
            {
                if (mostrarFormulario)
                {
                    login.MostrarFormulario();
                }
                login.TomarFormularioMostrado();
                PantallaConsola.Ejecutar(consola, loginControlador.IniciarSesion);
                // Si el inicio falla, se vuelve a pedir usuario y clave sin repetir el encabezado.
                mostrarFormulario = false;
                if (sesion.EstaActiva())
                {
                    while (sesion.EstaActiva())
                    {
                        Atender(menu.LeerOpcion());
                    }
                    // Al cerrar sesión el controlador ya mostró el formulario; si la sesión
                    // expiró, todavía no.
                    mostrarFormulario = !login.TomarFormularioMostrado();
                }
            }
        }
        catch (Consola.Salida)
        {
            consola.LineaEnBlanco();
            consola.Texto("Hasta luego.");
        }
    }

    private void Atender(int opcion)
    {
        switch (opcion)
        {
            case MenuConsola.IMPORTAR: importacion.Mostrar(); break;
            case MenuConsola.CONSULTAR: consulta.Mostrar(); break;
            case MenuConsola.PAISES: paises.Mostrar(); break;
            case MenuConsola.BUSCAR: busqueda.Mostrar(); break;
            case MenuConsola.REPORTES: reportes.Mostrar(); break;
            case MenuConsola.UNIVERSIDADES: universidades.Mostrar(); break;
            case MenuConsola.CERRAR_SESION: PantallaConsola.Ejecutar(consola, loginControlador.CerrarSesion); break;
            default:
                // MenuConsola ya mostró «Opción no válida».
                break;
        }
    }
}
