using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>Reportes (ECUD6) en reportes.cshtml.</summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ReporteVistaWeb : VistaWeb, ReporteVista
{
    public const string REPORTE = "reporte";

    public void MostrarReporte(Reporte reporte)
    {
        Poner(REPORTE, reporte);
    }
}
