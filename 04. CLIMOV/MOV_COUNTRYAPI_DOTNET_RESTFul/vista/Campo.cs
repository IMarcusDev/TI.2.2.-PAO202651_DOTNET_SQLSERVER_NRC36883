using Microsoft.Maui.Controls.Shapes;

namespace ec.edu.monster.vista;

/// <summary>
/// Campo de texto con su etiqueta arriba y un borde redondeado (el TextInputLayout de Android).
/// Opcionalmente lleva un ícono a la izquierda, el ojo para ver la clave (password_toggle) y el
/// botón para borrar el texto (clear_text).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Campo : ContentView
{
    private readonly Label etiqueta;
    private readonly Image icono;
    private readonly Border borde;
    private readonly Grid fila;
    private readonly ImageButton ojo;

    public Entry Entrada { get; }

    public Campo()
    {
        etiqueta = new Label { FontSize = 13, TextColor = Colores.De("TextoSuave"), Margin = new Thickness(2, 0, 0, 4) };
        icono = new Image { WidthRequest = 20, HeightRequest = 20, IsVisible = false, VerticalOptions = LayoutOptions.Center };
        Entrada = new Entry { FontSize = 16, TextColor = Colores.De("Texto"), VerticalOptions = LayoutOptions.Center };
        ojo = new ImageButton
        {
            Source = "ic_ver_clave_suave.png",
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = 10,
            BackgroundColor = Colors.Transparent,
            IsVisible = false,
            VerticalOptions = LayoutOptions.Center
        };
        SemanticProperties.SetDescription(ojo, "Mostrar clave");
        ojo.Clicked += (_, _) => AlternarClave();
        fila = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)]
        };
        fila.Add(icono, 0);
        fila.Add(Entrada, 1);
        fila.Add(ojo, 2);
        borde = new Border
        {
            Stroke = Colores.De("Borde"),
            StrokeThickness = 1,
            BackgroundColor = Colores.De("Blanco"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 2),
            MinimumHeightRequest = 52,
            Content = fila
        };
        Entrada.Focused += (_, _) => borde.Stroke = Colores.De("Azul");
        Entrada.Unfocused += (_, _) => borde.Stroke = Colores.De("Borde");
        Content = new VerticalStackLayout { Children = { etiqueta, borde } };
    }

    public string Etiqueta
    {
        get => etiqueta.Text;
        set
        {
            etiqueta.Text = value;
            SemanticProperties.SetDescription(Entrada, value);
        }
    }

    public string Texto
    {
        get => Entrada.Text ?? "";
        set => Entrada.Text = value;
    }

    public ImageSource? Icono
    {
        get => icono.Source;
        set
        {
            icono.Source = value;
            icono.IsVisible = value != null;
            fila.ColumnSpacing = value != null ? 10 : 0;
        }
    }

    /// <summary>Campo de clave: oculta el texto y muestra el ojo para verla.</summary>
    public bool EsClave
    {
        get => ojo.IsVisible;
        set
        {
            Entrada.IsPassword = value;
            ojo.IsVisible = value;
        }
    }

    /// <summary>Muestra la «x» para borrar el texto mientras se escribe.</summary>
    public bool BotonBorrar
    {
        get => Entrada.ClearButtonVisibility == ClearButtonVisibility.WhileEditing;
        set => Entrada.ClearButtonVisibility = value ? ClearButtonVisibility.WhileEditing : ClearButtonVisibility.Never;
    }

    public Keyboard Teclado
    {
        get => Entrada.Keyboard;
        set => Entrada.Keyboard = value;
    }

    public ReturnType TipoRetorno
    {
        get => Entrada.ReturnType;
        set => Entrada.ReturnType = value;
    }

    private void AlternarClave()
    {
        Entrada.IsPassword = !Entrada.IsPassword;
        ojo.Source = Entrada.IsPassword ? "ic_ver_clave_suave.png" : "ic_ocultar_clave_suave.png";
        SemanticProperties.SetDescription(ojo, Entrada.IsPassword ? "Mostrar clave" : "Ocultar clave");
    }
}
