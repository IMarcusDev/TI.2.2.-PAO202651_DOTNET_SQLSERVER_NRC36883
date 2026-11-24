namespace ec.edu.monster.vista;

/// <summary>
/// Aviso de la pantalla: verde con mensajes de éxito, rojo con errores. Oculto si no hay texto.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Aviso : ContentView
{
    private readonly Border caja;
    private readonly Label texto;

    public Aviso()
    {
        texto = new Label { FontSize = 14, FontFamily = "sans-serif-medium" };
        caja = new Border
        {
            Padding = new Thickness(12),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = texto
        };
        Content = caja;
        Margin = new Thickness(0, 12, 0, 0);
        IsVisible = false;
    }

    /// <summary>Pinta el aviso; un texto vacío lo oculta.</summary>
    public void Mostrar(string? mensaje, bool esError)
    {
        if (string.IsNullOrEmpty(mensaje))
        {
            Ocultar();
            return;
        }
        texto.Text = mensaje;
        texto.TextColor = Colores.De(esError ? "Rojo" : "VerdeTexto");
        caja.BackgroundColor = Colores.De(esError ? "RojoClaro" : "VerdeClaro");
        caja.Stroke = Colores.De(esError ? "Rojo" : "Verde");
        IsVisible = true;
        SemanticScreenReader.Announce(mensaje);
    }

    public void Ocultar()
    {
        texto.Text = "";
        IsVisible = false;
    }
}
