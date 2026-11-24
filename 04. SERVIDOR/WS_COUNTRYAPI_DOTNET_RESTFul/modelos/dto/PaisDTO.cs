using System.Text.Json.Serialization;
using ec.edu.monster.modelos.data;

namespace ec.edu.monster.modelos.dto;

/// <summary>
/// País tal como viaja entre el servidor y los clientes: la región va como texto y
/// «guardado» indica, en la consulta a la API, si el país ya está en la base de datos.
/// Las claves salen en el orden del servidor Java: id, nombre, capital, region, subregion,
/// poblacion, area, moneda, guardado.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class PaisDTO
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Capital { get; set; }
    public string? Region { get; set; }

    /// <summary>En el contrato se llama «subregion» (todo en minúsculas), como en Java.</summary>
    [JsonPropertyName("subregion")]
    public string? SubRegion { get; set; }

    public long Poblacion { get; set; }
    public double Area { get; set; }
    public string? Moneda { get; set; }
    public bool Guardado { get; set; }

    /// <summary>Copia los datos de un país; guardado indica si ese país ya existe en la base de datos.</summary>
    public static PaisDTO Desde(Pais pais, bool guardado)
    {
        return new PaisDTO
        {
            Id = pais.Id,
            Nombre = pais.Nombre,
            Capital = pais.Capital,
            Region = pais.NombreRegion,
            SubRegion = pais.SubRegion,
            Poblacion = pais.Poblacion,
            Area = pais.Area,
            Moneda = pais.Moneda,
            Guardado = guardado
        };
    }

    /// <summary>
    /// Convierte el DTO recibido del cliente en un país. La región queda solo con su nombre;
    /// el controlador la busca en el catálogo y completa su id. Un nombre ausente queda vacío,
    /// y la validación lo rechaza igual que un nombre en blanco.
    /// </summary>
    public Pais APais()
    {
        return new Pais(Nombre ?? "", Capital, Region == null ? null : new Region(0, Region),
            SubRegion, Poblacion, Area, Moneda)
        {
            Id = Id
        };
    }
}
