using ec.edu.monster.modelos;
using ec.edu.monster.servicios;
using ec.edu.monster.vista;

namespace ec.edu.monster.controlador;

/// <summary>
/// Importar y consultar universidades por país (ECUD10).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class UniversidadControlador
{
    internal const string SIN_UNIVERSIDADES = "No hay universidades guardadas para ese país";

    private readonly UniversidadVista vista;
    private readonly UniversidadServicio servicio;
    private readonly SesionServicio sesion;

    public UniversidadControlador(UniversidadVista vista, UniversidadServicio servicio, SesionServicio sesion)
    {
        this.vista = vista;
        this.servicio = servicio;
        this.sesion = sesion;
    }

    /// <summary>Importa las universidades del país escrito, muestra el resumen y recarga la tabla.</summary>
    public async Task Importar()
    {
        string? pais = vista.LeerPais();
        string? error = ValidadorDatos.ValidarPaisUniversidades(pais);
        if (error != null)
        {
            vista.MostrarError(error);
            return;
        }
        RespuestaDTO<ResumenImportacion> respuesta = await servicio.Importar(sesion.ObtenerToken(), pais!);
        if (Fallo(respuesta))
        {
            return;
        }
        await Cargar(pais!, false);
        vista.MostrarResumen(respuesta.Datos!);
    }

    /// <summary>Muestra las universidades guardadas del país escrito («Ver guardadas»).</summary>
    public async Task Listar()
    {
        string? pais = vista.LeerPais();
        string? error = ValidadorDatos.ValidarPaisUniversidades(pais);
        if (error != null)
        {
            vista.MostrarError(error);
            return;
        }
        await Cargar(pais!, true);
    }

    private async Task Cargar(string pais, bool avisarSiVacia)
    {
        RespuestaDTO<List<Universidad>> respuesta = await servicio.Listar(sesion.ObtenerToken(), pais);
        if (Fallo(respuesta))
        {
            return;
        }
        vista.MostrarUniversidades(respuesta.Datos!);
        if (avisarSiVacia && respuesta.Datos!.Count == 0)
        {
            vista.MostrarMensaje(SIN_UNIVERSIDADES);
        }
    }

    private bool Fallo<T>(RespuestaDTO<T> respuesta)
    {
        if (respuesta.EsOk())
        {
            return false;
        }
        vista.MostrarError(respuesta.Mensaje);
        if (respuesta.SesionExpirada)
        {
            sesion.Cerrar();
            vista.IrAInicioSesion();
        }
        return true;
    }
}
