using System.Windows.Input;
using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Universidades (ECUD10): país en inglés (sugerido «Ecuador»), botones Importar y Ver guardadas,
/// y la tabla con nombre, dominio y sitio web.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class UniversidadPanel : PanelBase, UniversidadVista
{
    public UniversidadPanel(VentanaPrincipal ventana)
    {
        InitializeComponent();
        Preparar(ventana, AlertaPantalla);
    }

    public void EstablecerControlador(UniversidadControlador controlador)
    {
        Importar.Click += (_, _) => Ejecutar(controlador.Importar);
        VerGuardadas.Click += (_, _) => Ejecutar(controlador.Listar);
        CampoPais.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Ejecutar(controlador.Listar);
            }
        };
    }

    public string? LeerPais()
    {
        return CampoPais.Text;
    }

    public void MostrarResumen(ResumenImportacion r)
    {
        MostrarMensaje("Importación de universidades finalizada: " + r.Recibidos + " recibidas, "
            + r.Registrados + " registradas y " + r.Actualizados + " actualizadas");
    }

    public void MostrarUniversidades(List<Universidad> universidades)
    {
        Tabla.ItemsSource = universidades;
        Total.Text = universidades.Count == 1 ? "1 universidad" : universidades.Count + " universidades";
    }
}
