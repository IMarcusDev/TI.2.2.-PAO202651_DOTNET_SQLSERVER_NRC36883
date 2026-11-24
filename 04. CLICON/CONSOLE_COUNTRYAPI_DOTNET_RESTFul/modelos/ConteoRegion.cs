namespace ec.edu.monster.modelos;

/// <summary>
/// Fila del reporte «Cantidad de países por región».
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ConteoRegion
{
    public string? Region { get; set; }
    public int Cantidad { get; set; }

    public ConteoRegion()
    {
    }

    public ConteoRegion(string? region, int cantidad)
    {
        Region = region;
        Cantidad = cantidad;
    }
}
