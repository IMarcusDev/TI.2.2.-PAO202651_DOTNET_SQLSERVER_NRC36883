using System.Windows;
using System.Windows.Media;

namespace ec.edu.monster.vista;

/// <summary>
/// Pregunta de confirmación con los textos de botón de TI.2.1 («Eliminar» / «Cancelar»,
/// «Importar» / «Cancelar»). MessageBox de WPF no permite cambiar el texto de los botones, por
/// eso se usa esta ventana pequeña.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class DialogoConfirmacion : Window
{
    private DialogoConfirmacion()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Muestra la pregunta y espera la respuesta.
    /// </summary>
    /// <param name="advertencia">true: icono de advertencia (rojo); false: de pregunta (azul)</param>
    /// <param name="aceptarPorDefecto">si Enter elige el primer botón o «Cancelar»</param>
    /// <returns>true si el usuario eligió el primer botón</returns>
    public static bool Preguntar(Window? duenio, string mensaje, string titulo, string textoAceptar,
        bool advertencia, bool aceptarPorDefecto)
    {
        var dialogo = new DialogoConfirmacion { Title = titulo, Owner = duenio };
        dialogo.Mensaje.Text = mensaje;
        dialogo.Aceptar.Content = textoAceptar;
        dialogo.Cancelar.Content = "Cancelar";
        dialogo.Simbolo.Text = advertencia ? "!" : "?";
        dialogo.Icono.Background = (Brush)dialogo.FindResource(advertencia ? "Rojo" : "Azul");
        dialogo.Aceptar.IsDefault = aceptarPorDefecto;
        dialogo.Cancelar.IsDefault = !aceptarPorDefecto;
        dialogo.Loaded += (_, _) => (aceptarPorDefecto ? dialogo.Aceptar : dialogo.Cancelar).Focus();
        return dialogo.ShowDialog() == true;
    }

    private void AlAceptar(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
