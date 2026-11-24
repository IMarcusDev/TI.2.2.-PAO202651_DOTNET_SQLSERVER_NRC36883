namespace ec.edu.monster.modelos.data;

/// <summary>
/// Fila de la tabla Universidades, importada desde la API de universidades (Hipolabs).
/// La pareja nombre y país es única. GET /api/Universidades la devuelve tal cual, con las
/// claves en el orden del servidor Java: id, nombre, dominio, sitioWeb, pais, fechaActualizacion.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Universidad
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Dominio { get; set; }
    public string? SitioWeb { get; set; }
    public string? Pais { get; set; }
    public DateTime FechaActualizacion { get; set; }

    public Universidad()
    {
    }

    public Universidad(string nombre, string? dominio, string? sitioWeb, string? pais)
    {
        Nombre = nombre;
        Dominio = dominio;
        SitioWeb = sitioWeb;
        Pais = pais;
    }
}
