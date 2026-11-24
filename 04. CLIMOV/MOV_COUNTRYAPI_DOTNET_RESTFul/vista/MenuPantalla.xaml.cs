using Microsoft.Maui.Controls.Shapes;

namespace ec.edu.monster.vista;

/// <summary>
/// Menú principal: una tarjeta por opción, en el orden del ECUD1, y «Cerrar sesión». La interfaz
/// MenuVista la implementa <see cref="PaginaPrincipal"/>, que es quien cambia de pantalla.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class MenuPantalla : PantallaBase
{
    public MenuPantalla()
    {
        InitializeComponent();
        Preparar(AvisoPantalla);
        Opcion("ic_importar_azul.png", "Importar países", "Trae todos los países desde la API REST Countries.", PaginaPrincipal.IMPORTAR);
        Opcion("ic_consultar_azul.png", "Consultar país en la API", "Busca un país por nombre en la API y guárdelo.", PaginaPrincipal.CONSULTAR);
        Opcion("ic_paises_azul.png", "Países", "Lista, registra, edita y elimina los países guardados.", PaginaPrincipal.PAISES);
        Opcion("ic_filtro_azul.png", "Buscar y filtrar", "Busca por nombre, capital y región con filtros dinámicos.", PaginaPrincipal.BUSCAR);
        Opcion("ic_reporte_azul.png", "Reportes", "Países por región, país más poblado y promedio poblacional.", PaginaPrincipal.REPORTES);
        Opcion("ic_universidad_azul.png", "Universidades", "Importa y consulta las universidades de un país.", PaginaPrincipal.UNIVERSIDADES);
        Opcion("ic_salir_azul.png", "Cerrar sesión", "Termina la sesión y vuelve al inicio.", null);
    }

    /// <summary>Saludo al entrar («Bienvenido, nombre.»).</summary>
    internal void Saludar(string? nombre)
    {
        MostrarMensaje("Bienvenido, " + nombre + ".");
    }

    private void Opcion(string icono, string titulo, string descripcion, string? pantalla)
    {
        var circulo = new Border
        {
            BackgroundColor = Colores.De("AzulClaro"),
            StrokeThickness = 0,
            StrokeShape = new Ellipse(),
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = 10,
            VerticalOptions = LayoutOptions.Center,
            Content = new Image { Source = icono }
        };
        var textos = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = titulo, FontSize = 17, FontFamily = "sans-serif-medium", TextColor = Colores.De("Azul") },
                new Label { Text = descripcion, Style = (Style)Application.Current!.Resources["Suave"], Margin = new Thickness(0, 2, 0, 0) }
            }
        };
        var fila = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)],
            ColumnSpacing = 16,
            Padding = 16,
            MinimumHeightRequest = 72
        };
        fila.Add(circulo, 0);
        fila.Add(textos, 1);
        var tarjeta = new Border
        {
            Style = (Style)Application.Current.Resources["Tarjeta"],
            Margin = new Thickness(0, 0, 0, 12),
            Content = fila
        };
        SemanticProperties.SetDescription(tarjeta, titulo + ". " + descripcion);
        var toque = new TapGestureRecognizer();
        toque.Tapped += (_, _) =>
        {
            if (pantalla == null)
            {
                Principal?.CerrarSesion();
            }
            else
            {
                Principal?.Mostrar(pantalla);
            }
        };
        tarjeta.GestureRecognizers.Add(toque);
        Opciones.Add(tarjeta);
    }
}
