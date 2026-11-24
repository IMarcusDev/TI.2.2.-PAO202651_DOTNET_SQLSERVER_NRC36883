using ec.edu.monster.modelos;
using ec.edu.monster.servicios;
using ec.edu.monster.vista;

namespace ec.edu.monster.controlador;

/// <summary>
/// Operaciones que pasan por la API REST Countries a través del servidor: importar todos los
/// países (ECUD2) y consultar un país por nombre para guardarlo (ECUD3).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ApiControlador
{
    internal const string SELECCIONE_PAIS = "Seleccione un país de la tabla";

    private readonly ConsultaApiVista vistaConsulta;
    private readonly ImportacionVista vistaImportacion;
    private readonly PaisServicio servicio;
    private readonly SesionServicio sesion;

    /// <summary>Última lista consultada, para marcar el país guardado sin volver a consultar.</summary>
    private List<Pais> resultados = [];

    public ApiControlador(ConsultaApiVista vistaConsulta, ImportacionVista vistaImportacion,
        PaisServicio servicio, SesionServicio sesion)
    {
        this.vistaConsulta = vistaConsulta;
        this.vistaImportacion = vistaImportacion;
        this.servicio = servicio;
        this.sesion = sesion;
    }

    /// <summary>Pide confirmación e importa todos los países; muestra el resumen.</summary>
    public async Task Importar()
    {
        if (!vistaImportacion.ConfirmarImportacion())
        {
            return;
        }
        RespuestaDTO<ResumenImportacion> respuesta = await servicio.Importar(sesion.ObtenerToken());
        if (Fallo(respuesta, vistaImportacion))
        {
            return;
        }
        vistaImportacion.MostrarResumen(respuesta.Datos!);
    }

    /// <summary>Consulta en la API los países que coinciden con el nombre (al menos dos caracteres).</summary>
    public async Task Consultar()
    {
        string? nombre = vistaConsulta.LeerNombre();
        string? error = ValidadorDatos.ValidarNombreConsulta(nombre);
        if (error != null)
        {
            vistaConsulta.MostrarError(error);
            return;
        }
        RespuestaDTO<List<Pais>> respuesta = await servicio.ConsultarExterno(sesion.ObtenerToken(), nombre!);
        if (Fallo(respuesta, vistaConsulta))
        {
            return;
        }
        resultados = respuesta.Datos!;
        vistaConsulta.MostrarResultados(resultados);
    }

    /// <summary>Guarda en la base el país elegido de los resultados (lo registra o lo actualiza).</summary>
    public async Task Guardar()
    {
        Pais? seleccionado = vistaConsulta.LeerSeleccion();
        if (seleccionado == null)
        {
            vistaConsulta.MostrarError(SELECCIONE_PAIS);
            return;
        }
        RespuestaDTO<Pais> respuesta = await servicio.Guardar(sesion.ObtenerToken(), seleccionado);
        if (Fallo(respuesta, vistaConsulta))
        {
            return;
        }
        seleccionado.Guardado = true;
        vistaConsulta.MostrarResultados(resultados);
        vistaConsulta.MostrarMensaje(respuesta.Mensaje);
    }

    private bool Fallo<T>(RespuestaDTO<T> respuesta, VistaBase vistaActual)
    {
        if (respuesta.EsOk())
        {
            return false;
        }
        vistaActual.MostrarError(respuesta.Mensaje);
        if (respuesta.SesionExpirada)
        {
            sesion.Cerrar();
            vistaActual.IrAInicioSesion();
        }
        return true;
    }
}
