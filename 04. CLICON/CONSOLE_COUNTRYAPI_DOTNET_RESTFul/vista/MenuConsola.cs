using ec.edu.monster.servicios;

namespace ec.edu.monster.vista;

/// <summary>
/// Menú principal numerado, en el orden del ECUD1. Al mostrarse indica si el servidor responde
/// (el indicador de los otros clientes).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class MenuConsola : PantallaConsola, MenuVista
{
    public const int IMPORTAR = 1;
    public const int CONSULTAR = 2;
    public const int PAISES = 3;
    public const int BUSCAR = 4;
    public const int REPORTES = 5;
    public const int UNIVERSIDADES = 6;
    public const int CERRAR_SESION = 7;

    private static readonly string[] OPCIONES =
    [
        "Importar países",
        "Consultar país en la API",
        "Países (listar, registrar, editar y eliminar)",
        "Buscar y filtrar",
        "Reportes",
        "Universidades",
        "Cerrar sesión"
    ];

    private readonly string urlServidor;
    private readonly Func<bool> servidorDisponible;

    public MenuConsola(Consola consola, SesionServicio sesion, string urlServidor, Func<bool> servidorDisponible)
        : base(consola, sesion)
    {
        this.urlServidor = urlServidor;
        this.servidorDisponible = servidorDisponible;
    }

    public void MostrarMenu(string? nombre)
    {
        consola.Mensaje("Bienvenido, " + nombre + ".");
    }

    public int LeerOpcion()
    {
        consola.Titulo("Menú principal · " + sesion.Nombre);
        consola.Ayuda(EstadoServidor());
        for (int i = 0; i < OPCIONES.Length; i++)
        {
            consola.Texto((i + 1) + ". " + OPCIONES[i]);
        }
        int opcion = consola.LeerOpcion("Seleccione una opción");
        if (opcion < IMPORTAR || opcion > CERRAR_SESION)
        {
            consola.Error(OPCION_NO_VALIDA);
        }
        return opcion;
    }

    /// <summary>«Servidor: http://... · Servidor disponible» o «... · Servidor no disponible».</summary>
    internal string EstadoServidor()
    {
        return "Servidor: " + urlServidor + " · "
            + (servidorDisponible() ? "Servidor disponible" : "Servidor no disponible");
    }
}
