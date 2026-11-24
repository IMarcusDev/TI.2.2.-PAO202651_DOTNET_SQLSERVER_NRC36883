using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Reportes estadísticos (ECUD6).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface ReporteVista : VistaBase
{
    void MostrarReporte(Reporte reporte);
}
