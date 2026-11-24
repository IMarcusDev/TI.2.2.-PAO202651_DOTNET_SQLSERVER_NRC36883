using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Países (ECUD4): DataGrid con los siete datos, campo «Buscar» y botones Nuevo, Editar y
/// Eliminar. Doble clic en una fila también abre «Editar».
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class PaisPanel : PanelBase, PaisVista
{
    public PaisPanel(VentanaPrincipal ventana)
    {
        InitializeComponent();
        Preparar(ventana, AlertaPantalla);
    }

    /// <summary>Conecta los botones con el controlador.</summary>
    public void EstablecerControlador(PaisControlador controlador)
    {
        BotonBuscar.Click += (_, _) => Ejecutar(controlador.Buscar);
        Filtro.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Ejecutar(controlador.Buscar);
            }
        };
        Nuevo.Click += (_, _) => Ejecutar(controlador.Registrar);
        Editar.Click += (_, _) => Ejecutar(controlador.Actualizar);
        Eliminar.Click += (_, _) => Ejecutar(controlador.Eliminar);
        Tabla.MouseDoubleClick += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Left && EnUnaFila(e.OriginalSource as DependencyObject))
            {
                Ejecutar(controlador.Actualizar);
            }
        };
    }

    private static bool EnUnaFila(DependencyObject? origen)
    {
        while (origen != null && origen is not DataGridRow)
        {
            origen = origen is Visual ? VisualTreeHelper.GetParent(origen) : LogicalTreeHelper.GetParent(origen);
        }
        return origen != null;
    }

    public void MostrarTabla(List<Pais> paises)
    {
        Tabla.ItemsSource = paises;
        Total.Text = paises.Count == 1 ? "1 país" : paises.Count + " países";
    }

    public string? LeerFiltro()
    {
        return Filtro.Text;
    }

    public Pais? LeerPais(Pais? inicial)
    {
        return FormularioPaisDialogo.Pedir(Window.GetWindow(this), inicial);
    }

    public Pais? PaisSeleccionado()
    {
        return Tabla.SelectedItem as Pais;
    }

    public bool ConfirmarEliminacion(Pais pais)
    {
        return DialogoConfirmacion.Preguntar(Window.GetWindow(this), "¿Eliminar el país " + pais.Nombre + "?",
            "Eliminar país", "Eliminar", advertencia: true, aceptarPorDefecto: false);
    }
}
