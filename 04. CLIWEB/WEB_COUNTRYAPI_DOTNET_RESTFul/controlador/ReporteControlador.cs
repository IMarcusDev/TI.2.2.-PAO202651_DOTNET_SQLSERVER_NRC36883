using ec.edu.monster.modelos;
using ec.edu.monster.servicios;
using ec.edu.monster.vista;

namespace ec.edu.monster.controlador;

/// <summary>
/// Reportes estadísticos (ECUD6).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ReporteControlador
{
    internal const string SUGERENCIA_IMPORTAR = ". Use «Importar países» para cargarlos.";

    private readonly ReporteVista vista;
    private readonly ReporteServicio servicio;
    private readonly SesionServicio sesion;

    public ReporteControlador(ReporteVista vista, ReporteServicio servicio, SesionServicio sesion)
    {
        this.vista = vista;
        this.servicio = servicio;
        this.sesion = sesion;
    }

    /// <summary>Pide los tres reportes; si no hay países, sugiere importarlos (ECUD6, flujo alternativo 2).</summary>
    public async Task Generar()
    {
        RespuestaDTO<Reporte> respuesta = await servicio.Generar(sesion.ObtenerToken());
        if (respuesta.EsOk())
        {
            vista.MostrarReporte(respuesta.Datos!);
            return;
        }
        if (respuesta.CodigoHttp == 404)
        {
            vista.MostrarError(respuesta.Mensaje + SUGERENCIA_IMPORTAR);
            return;
        }
        vista.MostrarError(respuesta.Mensaje);
        if (respuesta.SesionExpirada)
        {
            sesion.Cerrar();
            vista.IrAInicioSesion();
        }
    }
}
