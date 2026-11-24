using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.dto;
using ec.edu.monster.modelos.interfaces;
using ec.edu.monster.servicios;

namespace ec.edu.monster.controladores;

/// <summary>
/// Importación y consulta de universidades (RQF-010, ECUD10).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class UniversidadControlador
{
    internal const string INGRESE_PAIS = "Ingrese el país";
    internal const string SIN_UNIVERSIDADES = "No se encontraron universidades para ese país";

    private readonly IUniversidadDAO universidadDAO;
    private readonly ConsumoAPIUniversidades consumoAPI;

    public UniversidadControlador(IUniversidadDAO universidadDAO, ConsumoAPIUniversidades consumoAPI)
    {
        this.universidadDAO = universidadDAO;
        this.consumoAPI = consumoAPI;
    }

    /// <summary>
    /// Consulta la API con el país (en inglés) y registra o actualiza sus universidades en una
    /// transacción. Si la API falla o no devuelve universidades, la base no cambia.
    /// </summary>
    public async Task<ResumenImportacion> ImportarUniversidades(string? pais)
    {
        ValidarPais(pais);
        List<Universidad> universidades = await consumoAPI.ObtenerPorPais(pais!);
        if (universidades.Count == 0)
        {
            throw new KeyNotFoundException(SIN_UNIVERSIDADES);
        }
        return await universidadDAO.GuardarTodas(universidades);
    }

    /// <summary>Universidades guardadas del país, ordenadas por nombre (lista vacía si no hay).</summary>
    public List<Universidad> ListarUniversidades(string? pais)
    {
        ValidarPais(pais);
        return universidadDAO.ListarPorPais(pais!);
    }

    private static void ValidarPais(string? pais)
    {
        if (string.IsNullOrWhiteSpace(pais))
        {
            throw new ArgumentException(INGRESE_PAIS);
        }
    }
}
