using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Reportes estadísticos (ECUD6): países por región, país más poblado y promedio poblacional por
/// región. Se generan al entrar y con el botón «Actualizar».
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class ReportePanel : PanelBase, ReporteVista
{
    public ReportePanel(VentanaPrincipal ventana)
    {
        InitializeComponent();
        Preparar(ventana, AlertaPantalla);
    }

    public void EstablecerControlador(ReporteControlador controlador)
    {
        Actualizar.Click += (_, _) => Ejecutar(controlador.Generar);
    }

    public void MostrarReporte(Reporte reporte)
    {
        Conteo.ItemsSource = reporte.PaisesPorRegion;
        Promedio.ItemsSource = reporte.PromedioPorRegion;
        Pais? pais = reporte.PaisMasPoblado;
        MasPoblado.Text = pais == null ? "—" : pais.Nombre;
        PoblacionMasPoblado.Text = pais == null
            ? " "
            : Estilos.FormatearEntero(pais.Poblacion) + " habitantes · " + pais.Region;
    }
}
