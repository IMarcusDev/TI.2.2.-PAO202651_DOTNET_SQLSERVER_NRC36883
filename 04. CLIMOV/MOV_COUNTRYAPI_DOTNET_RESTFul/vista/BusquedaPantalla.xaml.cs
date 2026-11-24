using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// «Buscar y filtrar» (ECUD5): nombre y capital parciales y un filtro de región. Elegir una
/// región vuelve a consultar al momento.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class BusquedaPantalla : PantallaBase, BusquedaVista
{
    private const string TODAS = "Todas";

    private readonly PaisAdaptador adaptador;
    private PaisControlador? controlador;
    private List<string> regiones = [];
    /// <summary>Región elegida, o null para «Todas».</summary>
    private string? regionElegida;

    public BusquedaPantalla()
    {
        InitializeComponent();
        Preparar(AvisoPantalla);
        adaptador = new PaisAdaptador(ListaBusqueda, false);
        BotonBuscar.Clicked += (_, _) => Buscar();
        CampoNombre.Entrada.Completed += (_, _) => CampoCapital.Entrada.Focus();
        CampoCapital.Entrada.Completed += (_, _) => Buscar();
    }

    internal void EstablecerControlador(PaisControlador paisControlador)
    {
        controlador = paisControlador;
    }

    private void Buscar()
    {
        if (controlador != null)
        {
            Ejecutar(controlador.BuscarYFiltrar);
        }
    }

    /// <summary>«Todas» más una opción por región; se vuelve a consultar al elegir otra.</summary>
    private void PintarFiltros()
    {
        Filtros.Clear();
        Filtros.Add(Chip(TODAS, null));
        foreach (string region in regiones)
        {
            Filtros.Add(Chip(region, region));
        }
    }

    private Button Chip(string texto, string? region)
    {
        bool elegido = region == regionElegida;
        var chip = new Button
        {
            Text = texto,
            FontSize = 14,
            FontFamily = "sans-serif-medium",
            HeightRequest = 36,
            Padding = new Thickness(16, 0),
            CornerRadius = 8,
            BorderWidth = 1,
            BackgroundColor = Colores.De(elegido ? "Azul" : "Blanco"),
            TextColor = Colores.De(elegido ? "Blanco" : "Azul"),
            BorderColor = Colores.De(elegido ? "Azul" : "Borde")
        };
        SemanticProperties.SetDescription(chip, texto + (elegido ? ", elegido" : ""));
        chip.Clicked += (_, _) =>
        {
            // Como el ChipGroup con una sola opción obligatoria: tocar la elegida no cambia nada.
            if (region == regionElegida)
            {
                return;
            }
            regionElegida = region;
            PintarFiltros();
            Buscar();
        };
        return chip;
    }

    // ------------------------------------------------------------------ BusquedaVista

    public CriteriosBusqueda LeerCriterios()
    {
        return Hilos.EnPrincipalYEsperar(() => new CriteriosBusqueda(
            Formato.VacioANulo(CampoNombre.Texto),
            Formato.VacioANulo(CampoCapital.Texto),
            regionElegida));
    }

    public void MostrarFiltrosRegion(IReadOnlyList<string> lista)
    {
        Hilos.EnPrincipal(() =>
        {
            regiones = [.. lista];
            PintarFiltros();
        });
    }

    public void MostrarResultados(List<Pais> paises, int total)
    {
        Hilos.EnPrincipal(() =>
        {
            adaptador.SetPaises(paises);
            TotalBusqueda.Text = Formato.Cantidad(total, "país encontrado", "países encontrados");
        });
    }
}
