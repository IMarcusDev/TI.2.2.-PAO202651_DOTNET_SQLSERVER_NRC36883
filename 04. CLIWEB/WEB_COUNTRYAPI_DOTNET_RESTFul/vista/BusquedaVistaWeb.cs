using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// «Buscar y filtrar» (ECUD5) en buscar.cshtml: nombre y capital parciales y una región. Al
/// elegir otra región el formulario se envía solo y se vuelve a consultar.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class BusquedaVistaWeb : VistaWeb, BusquedaVista
{
    public const string RESULTADOS = "resultados";
    public const string TOTAL = "total";

    /// <summary>Regiones para los filtros (las fija el controlador al crearse).</summary>
    public List<string> Regiones { get; private set; } = [];

    public CriteriosBusqueda LeerCriterios()
    {
        return new CriteriosBusqueda(ParametroConValor("nombre"), ParametroConValor("capital"), ParametroConValor("region"));
    }

    public void MostrarFiltrosRegion(IReadOnlyList<string> regiones)
    {
        Regiones = [.. regiones];
    }

    public void MostrarResultados(List<Pais> paises, int total)
    {
        Poner(RESULTADOS, paises);
        Poner(TOTAL, total);
    }
}
