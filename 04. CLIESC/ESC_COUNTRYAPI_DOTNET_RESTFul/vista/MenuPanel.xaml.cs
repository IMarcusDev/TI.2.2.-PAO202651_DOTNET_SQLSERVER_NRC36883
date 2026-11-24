using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ec.edu.monster.vista;

/// <summary>
/// Menú principal con una tarjeta por opción (como el menú de tarjetas del diseño web). Cada
/// tarjeta es un botón, así que también se usa con el teclado (Tab y Enter).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class MenuPanel : PanelBase
{
    public MenuPanel(VentanaPrincipal ventana)
    {
        InitializeComponent();
        Preparar(ventana, AlertaPantalla);
    }

    internal void Saludar(string? nombre)
    {
        MostrarMensaje("Bienvenido, " + nombre + ".");
    }

    internal void AgregarOpcion(string nombre, string descripcion, Action accion)
    {
        var contenido = new StackPanel { Width = 300 };
        contenido.Children.Add(new TextBlock
        {
            Text = nombre,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("Azul")
        });
        contenido.Children.Add(new TextBlock
        {
            Text = descripcion,
            Margin = new Thickness(0, 12, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)FindResource("TextoSuave")
        });
        var tarjeta = new Button
        {
            Content = contenido,
            Style = (Style)FindResource("TarjetaMenu"),
            Margin = new Thickness(0, 0, 16, 16)
        };
        tarjeta.Click += (_, _) => accion();
        Tarjetas.Children.Add(tarjeta);
    }
}
