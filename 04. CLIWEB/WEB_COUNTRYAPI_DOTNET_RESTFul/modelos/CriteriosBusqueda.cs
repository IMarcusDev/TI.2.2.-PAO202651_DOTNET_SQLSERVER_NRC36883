namespace ec.edu.monster.modelos;

/// <summary>
/// Criterios de «Buscar y filtrar»: nombre y capital (parciales) y región (exacta).
/// Un criterio vacío o null no se envía.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class CriteriosBusqueda
{
    public string? Nombre { get; set; }
    public string? Capital { get; set; }
    public string? Region { get; set; }

    public CriteriosBusqueda()
    {
    }

    public CriteriosBusqueda(string? nombre, string? capital, string? region)
    {
        Nombre = nombre;
        Capital = capital;
        Region = region;
    }
}
