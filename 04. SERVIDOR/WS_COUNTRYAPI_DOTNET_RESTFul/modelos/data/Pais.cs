using System.ComponentModel.DataAnnotations.Schema;

namespace ec.edu.monster.modelos.data;

/// <summary>
/// Fila de la tabla Paises con los siete datos del taller: nombre, capital, región, subregión,
/// población, área (km²) y moneda. La región es un objeto del catálogo Regiones.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Pais
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Capital { get; set; }
    public int IdRegion { get; set; }
    public Region? Region { get; set; }
    public string? SubRegion { get; set; }
    public long Poblacion { get; set; }
    public double Area { get; set; }
    public string? Moneda { get; set; }
    public DateTime FechaActualizacion { get; set; }

    public Pais()
    {
    }

    public Pais(string nombre, string? capital, Region? region, string? subRegion,
        long poblacion, double area, string? moneda)
    {
        Nombre = nombre;
        Capital = capital;
        AsignarRegion(region);
        SubRegion = subRegion;
        Poblacion = poblacion;
        Area = area;
        Moneda = moneda;
    }

    /// <summary>Nombre de la región o null si el país todavía no la tiene.</summary>
    [NotMapped]
    public string? NombreRegion => Region?.Nombre;

    /// <summary>Asigna la región y su id (la clave foránea IdRegion).</summary>
    public void AsignarRegion(Region? region)
    {
        Region = region;
        IdRegion = region?.IdRegion ?? 0;
    }
}
