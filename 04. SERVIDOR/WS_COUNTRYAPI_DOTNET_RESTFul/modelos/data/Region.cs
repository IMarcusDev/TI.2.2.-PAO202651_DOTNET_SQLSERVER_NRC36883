namespace ec.edu.monster.modelos.data;

/// <summary>
/// Fila del catálogo Regiones (América, Europa, Asia, África, Oceanía, Antártida).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Region
{
    public int IdRegion { get; set; }
    public string Nombre { get; set; } = "";

    public Region()
    {
    }

    public Region(int idRegion, string nombre)
    {
        IdRegion = idRegion;
        Nombre = nombre;
    }
}
