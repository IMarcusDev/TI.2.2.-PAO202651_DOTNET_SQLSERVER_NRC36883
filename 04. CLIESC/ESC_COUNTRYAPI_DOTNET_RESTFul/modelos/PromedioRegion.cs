namespace ec.edu.monster.modelos;

/// <summary>
/// Fila del reporte «Promedio poblacional por región».
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class PromedioRegion
{
    public string? Region { get; set; }
    public long PromedioPoblacion { get; set; }

    public PromedioRegion()
    {
    }

    public PromedioRegion(string? region, long promedioPoblacion)
    {
        Region = region;
        PromedioPoblacion = promedioPoblacion;
    }
}
