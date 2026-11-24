namespace ec.edu.monster.modelos;

/// <summary>
/// Universidad guardada: nombre, dominio, sitio web y país.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Universidad
{
    public string? Nombre { get; set; }
    public string? Dominio { get; set; }
    public string? SitioWeb { get; set; }
    public string? Pais { get; set; }

    public Universidad()
    {
    }

    public Universidad(string? nombre, string? dominio, string? sitioWeb, string? pais)
    {
        Nombre = nombre;
        Dominio = dominio;
        SitioWeb = sitioWeb;
        Pais = pais;
    }
}
