namespace ec.edu.monster.modelos;

/// <summary>
/// Los tres reportes estadísticos: países por región, país más poblado y promedio por región.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Reporte
{
    public List<ConteoRegion> PaisesPorRegion { get; set; } = [];
    public Pais? PaisMasPoblado { get; set; }
    public List<PromedioRegion> PromedioPorRegion { get; set; } = [];

    public Reporte()
    {
    }

    public Reporte(List<ConteoRegion> paisesPorRegion, Pais? paisMasPoblado, List<PromedioRegion> promedioPorRegion)
    {
        PaisesPorRegion = paisesPorRegion;
        PaisMasPoblado = paisMasPoblado;
        PromedioPorRegion = promedioPorRegion;
    }
}
