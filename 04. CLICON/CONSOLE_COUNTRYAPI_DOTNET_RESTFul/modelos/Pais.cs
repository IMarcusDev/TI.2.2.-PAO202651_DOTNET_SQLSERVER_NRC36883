using Newtonsoft.Json;

namespace ec.edu.monster.modelos;

/// <summary>
/// País con los siete datos del taller, tal como lo envía el servidor. «Guardado» indica, en la
/// consulta a la API, si el país ya está en la base de datos.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Pais
{
    /// <summary>Regiones del catálogo, en el orden en que se muestran en filtros y formularios.</summary>
    public static readonly IReadOnlyList<string> REGIONES =
        ["América", "Europa", "Asia", "África", "Oceanía", "Antártida"];

    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Capital { get; set; }
    public string? Region { get; set; }

    /// <summary>En el contrato se llama «subregion» (todo en minúsculas).</summary>
    [JsonProperty("subregion")]
    public string? SubRegion { get; set; }

    public long Poblacion { get; set; }
    public double Area { get; set; }
    public string? Moneda { get; set; }
    public bool Guardado { get; set; }

    public Pais()
    {
    }

    public Pais(string? nombre, string? capital, string? region, string? subRegion,
        long poblacion, double area, string? moneda)
    {
        Nombre = nombre;
        Capital = capital;
        Region = region;
        SubRegion = subRegion;
        Poblacion = poblacion;
        Area = area;
        Moneda = moneda;
    }

    /// <summary>Copia independiente, para editar sin tocar la fila de la tabla.</summary>
    public Pais Copia()
    {
        return new Pais(Nombre, Capital, Region, SubRegion, Poblacion, Area, Moneda)
        {
            Id = Id,
            Guardado = Guardado
        };
    }
}
