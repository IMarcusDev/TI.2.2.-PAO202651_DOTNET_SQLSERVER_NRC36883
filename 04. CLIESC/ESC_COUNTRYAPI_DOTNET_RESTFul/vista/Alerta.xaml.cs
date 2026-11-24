using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ec.edu.monster.vista;

/// <summary>
/// Aviso dentro de la pantalla: verde para éxito, rojo para error (como las alertas del diseño
/// web). Se oculta al cambiar de pantalla. Los mensajes largos ocupan varias líneas.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class Alerta : UserControl
{
    public Alerta()
    {
        InitializeComponent();
    }

    public void Exito(string? mensaje)
    {
        Mostrar(mensaje, "VerdeClaro", "Verde", "VerdeTexto");
    }

    public void Error(string? mensaje)
    {
        Mostrar(mensaje, "RojoClaro", "Rojo", "Rojo");
    }

    public void Ocultar()
    {
        Visibility = Visibility.Collapsed;
    }

    /// <summary>Texto que se está mostrando (lo usan las pruebas y el inicio de sesión).</summary>
    public string Mensaje => Texto.Text;

    private void Mostrar(string? mensaje, string fondo, string borde, string letra)
    {
        Texto.Text = mensaje ?? "";
        Texto.Foreground = (Brush)FindResource(letra);
        Marco.Background = (Brush)FindResource(fondo);
        Marco.BorderBrush = (Brush)FindResource(borde);
        Visibility = Visibility.Visible;
    }
}
