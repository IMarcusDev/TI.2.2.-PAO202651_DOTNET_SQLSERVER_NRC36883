using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Reportes (ECUD6): país más poblado, cantidad de países por región y promedio poblacional por
/// región. Se generan al entrar y con «Actualizar».
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class ReportePantalla : PantallaBase, ReporteVista
{
    private ReporteControlador? controlador;

    public ReportePantalla()
    {
        InitializeComponent();
        Preparar(AvisoPantalla);
        BotonActualizar.Clicked += (_, _) =>
        {
            if (controlador != null)
            {
                Ejecutar(controlador.Generar);
            }
        };
        Pintar(null);
    }

    internal void EstablecerControlador(ReporteControlador reporteControlador)
    {
        controlador = reporteControlador;
    }

    private void Pintar(Reporte? reporte)
    {
        Pais? pais = reporte?.PaisMasPoblado;
        MasPoblado.Text = pais == null ? "—" : pais.Nombre;
        PoblacionMasPoblado.Text = pais == null ? "" : Formato.Entero(pais.Poblacion) + " habitantes · " + pais.Region;

        var conteo = (reporte?.PaisesPorRegion ?? [])
            .Select(f => (f.Region ?? "", Formato.Entero(f.Cantidad))).ToList();
        Tabla.Llenar(TablaConteo, "Región", "Países", conteo);

        var promedio = (reporte?.PromedioPorRegion ?? [])
            .Select(f => (f.Region ?? "", Formato.Entero(f.PromedioPoblacion))).ToList();
        Tabla.Llenar(TablaPromedio, "Región", "Promedio de población", promedio);
    }

    // ------------------------------------------------------------------ ReporteVista

    public void MostrarReporte(Reporte reporte)
    {
        Hilos.EnPrincipal(() => Pintar(reporte));
    }
}
