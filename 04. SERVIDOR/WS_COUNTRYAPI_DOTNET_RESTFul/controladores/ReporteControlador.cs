using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.dto;
using ec.edu.monster.modelos.interfaces;

namespace ec.edu.monster.controladores;

/// <summary>
/// Reportes estadísticos sobre los países almacenados (RQF-006, ECUD6).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ReporteControlador
{
    internal const string SIN_PAISES = "No hay países registrados para generar reportes";

    private readonly IPaisDAO paisDAO;

    public ReporteControlador(IPaisDAO paisDAO)
    {
        this.paisDAO = paisDAO;
    }

    /// <summary>
    /// Calcula, con consultas de agregación, la cantidad de países por región, el país más
    /// poblado y el promedio poblacional por región.
    /// </summary>
    /// <exception cref="KeyNotFoundException">si no hay países almacenados (404)</exception>
    public ReporteDTO GenerarReportes()
    {
        List<ConteoRegion> paisesPorRegion = paisDAO.ContarPorRegion();
        int total = paisesPorRegion.Sum(c => c.Cantidad);
        if (total == 0)
        {
            throw new KeyNotFoundException(SIN_PAISES);
        }
        Pais? masPoblado = paisDAO.PaisMasPoblado();
        return new ReporteDTO(
            paisesPorRegion,
            masPoblado == null ? null : PaisDTO.Desde(masPoblado, true),
            paisDAO.PromedioPoblacionPorRegion());
    }
}
