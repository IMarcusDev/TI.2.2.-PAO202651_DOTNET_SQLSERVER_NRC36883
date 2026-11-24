namespace ec.edu.monster.modelos.dto;

/// <summary>
/// Los tres reportes estadísticos en una sola respuesta (RQF-006): países por región,
/// país más poblado y promedio poblacional por región.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ReporteDTO
{
    public List<ConteoRegion> PaisesPorRegion { get; set; } = [];
    public PaisDTO? PaisMasPoblado { get; set; }
    public List<PromedioRegion> PromedioPorRegion { get; set; } = [];

    public ReporteDTO()
    {
    }

    public ReporteDTO(List<ConteoRegion> paisesPorRegion, PaisDTO? paisMasPoblado,
        List<PromedioRegion> promedioPorRegion)
    {
        PaisesPorRegion = paisesPorRegion;
        PaisMasPoblado = paisMasPoblado;
        PromedioPorRegion = promedioPorRegion;
    }
}
