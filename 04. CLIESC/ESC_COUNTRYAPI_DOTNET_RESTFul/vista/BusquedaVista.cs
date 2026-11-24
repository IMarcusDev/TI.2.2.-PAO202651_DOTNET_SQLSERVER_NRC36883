using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// «Buscar y filtrar»: nombre, capital y filtros de región que se aplican al momento (ECUD5).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface BusquedaVista : VistaBase
{
    CriteriosBusqueda LeerCriterios();

    /// <summary>Muestra los filtros de región: «Todas» más las regiones recibidas.</summary>
    void MostrarFiltrosRegion(IReadOnlyList<string> regiones);

    void MostrarResultados(List<Pais> paises, int total);
}
