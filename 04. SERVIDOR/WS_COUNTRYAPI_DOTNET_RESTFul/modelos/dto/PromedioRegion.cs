namespace ec.edu.monster.modelos.dto;

/// <summary>
/// Fila del reporte «Promedio poblacional por región», redondeado a entero.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class PromedioRegion
{
    public string Region { get; set; } = "";
    public long PromedioPoblacion { get; set; }

    public PromedioRegion()
    {
    }

    public PromedioRegion(string region, long promedioPoblacion)
    {
        Region = region;
        PromedioPoblacion = promedioPoblacion;
    }
}
