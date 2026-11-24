using System.Diagnostics;
using ec.edu.monster.controlador;
using ec.edu.monster.servicios;
using ec.edu.monster.vista;

namespace ec.edu.monster.web;

/// <summary>
/// Lo que cada usuario tiene en su sesión: su <see cref="SesionServicio"/> (con el token del
/// servidor), sus vistas y sus controladores, armados igual que App.Construir en el cliente de
/// escritorio.
///
/// Los controladores son los mismos de los otros clientes. Para atender una petición,
/// <see cref="Ejecutar"/> enlaza las vistas con esa petición, corre la acción del controlador y
/// las suelta. Las peticiones de un mismo usuario se atienden de una en una.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ModulosSesion
{
    /// <summary>Nombre del atributo de la sesión.</summary>
    public const string ATRIBUTO = "modulos";

    /// <summary>Evita que dos peticiones simultáneas del mismo usuario creen módulos distintos.</summary>
    private static readonly Lock CREAR = new();

    private readonly SemaphoreSlim unaALaVez = new(1, 1);
    private readonly List<VistaWeb> vistas;

    public SesionServicio Sesion { get; } = new();

    public LoginVistaWeb Login { get; } = new();
    public MenuVistaWeb Menu { get; } = new();
    public ImportacionVistaWeb Importacion { get; } = new();
    public ConsultaApiVistaWeb Consulta { get; } = new();
    public PaisVistaWeb Paises { get; } = new();
    public BusquedaVistaWeb Busqueda { get; } = new();
    public ReporteVistaWeb Reportes { get; } = new();
    public UniversidadVistaWeb Universidades { get; } = new();

    public LoginControlador LoginControlador { get; }
    public ApiControlador ApiControlador { get; }
    public PaisControlador PaisControlador { get; }
    public ReporteControlador ReporteControlador { get; }
    public UniversidadControlador UniversidadControlador { get; }

    public ModulosSesion(ClienteRest cliente)
    {
        vistas = [Login, Menu, Importacion, Consulta, Paises, Busqueda, Reportes, Universidades];
        var paisServicio = new PaisServicio(cliente);
        LoginControlador = new LoginControlador(Login, Menu, new LoginServicio(cliente), Sesion);
        ApiControlador = new ApiControlador(Consulta, Importacion, paisServicio, Sesion);
        PaisControlador = new PaisControlador(Paises, Busqueda, paisServicio, Sesion);
        ReporteControlador = new ReporteControlador(Reportes, new ReporteServicio(cliente), Sesion);
        UniversidadControlador = new UniversidadControlador(Universidades, new UniversidadServicio(cliente), Sesion);
    }

    /// <summary>Los módulos del usuario; si no tiene sesión o módulos, se crean.</summary>
    public static ModulosSesion De(HttpContext contexto)
    {
        SesionWeb sesion = Almacen(contexto).Obtener(contexto, true)!;
        lock (CREAR)
        {
            if (sesion.Leer(ATRIBUTO) is not ModulosSesion modulos)
            {
                modulos = new ModulosSesion(contexto.RequestServices.GetRequiredService<ClienteRest>());
                sesion.Poner(ATRIBUTO, modulos);
            }
            return modulos;
        }
    }

    /// <summary>Los módulos del usuario sin crear nada, o null.</summary>
    public static ModulosSesion? Existente(HttpContext contexto)
    {
        return Almacen(contexto).Obtener(contexto, false)?.Leer(ATRIBUTO) as ModulosSesion;
    }

    /// <summary>True si hay un usuario con sesión abierta (token del servidor).</summary>
    public static bool ConSesion(HttpContext contexto)
    {
        return Existente(contexto)?.Sesion.EstaActiva() == true;
    }

    /// <summary>
    /// Ejecuta una acción del controlador con las vistas enlazadas a esta petición. Un error
    /// inesperado se muestra como aviso y no rompe la página.
    /// </summary>
    public async Task Ejecutar(HttpContext contexto, Func<Task> accion)
    {
        if (contexto.Request.HasFormContentType)
        {
            // Las vistas leen el formulario de forma síncrona: se carga antes.
            await contexto.Request.ReadFormAsync();
        }
        await unaALaVez.WaitAsync();
        vistas.ForEach(vista => vista.Atender(contexto));
        try
        {
            await accion();
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error inesperado en una acción: " + e);
            contexto.Items[VistaWeb.ERROR] = ClienteRest.RESPUESTA_NO_VALIDA;
        }
        finally
        {
            vistas.ForEach(vista => vista.Terminar());
            unaALaVez.Release();
        }
    }

    private static AlmacenSesiones Almacen(HttpContext contexto)
    {
        return contexto.RequestServices.GetRequiredService<AlmacenSesiones>();
    }
}
