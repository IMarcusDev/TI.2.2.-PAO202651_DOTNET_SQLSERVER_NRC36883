using System.Windows;
using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// «Importar países» (ECUD2): pide confirmación, importa todos los países de REST Countries y
/// muestra cuántos se recibieron, registraron y actualizaron.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class ImportacionPanel : PanelBase, ImportacionVista
{
    public ImportacionPanel(VentanaPrincipal ventana)
    {
        InitializeComponent();
        Preparar(ventana, AlertaPantalla);
    }

    public void EstablecerControlador(ApiControlador controlador)
    {
        Importar.Click += (_, _) => Ejecutar(controlador.Importar);
    }

    public bool ConfirmarImportacion()
    {
        return DialogoConfirmacion.Preguntar(Window.GetWindow(this),
            "¿Importar los países desde la API REST Countries? Los países existentes se actualizarán",
            "Importar países", "Importar", advertencia: false, aceptarPorDefecto: true);
    }

    public void MostrarResumen(ResumenImportacion r)
    {
        Recibidos.Text = Estilos.FormatearEntero(r.Recibidos);
        Registrados.Text = Estilos.FormatearEntero(r.Registrados);
        Actualizados.Text = Estilos.FormatearEntero(r.Actualizados);
        Resumen.Visibility = Visibility.Visible;
        MostrarMensaje("Importación finalizada: " + r.Recibidos + " países recibidos, "
            + r.Registrados + " registrados y " + r.Actualizados + " actualizados");
    }
}
