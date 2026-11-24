using ec.edu.monster.controlador;
using ec.edu.monster.modelos;
using Microsoft.Maui.Controls.Shapes;

namespace ec.edu.monster.vista;

/// <summary>
/// Universidades (ECUD10): importa desde la API de universidades las de un país (en inglés) y
/// muestra las guardadas.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class UniversidadPantalla : PantallaBase, UniversidadVista
{
    private UniversidadControlador? controlador;

    public UniversidadPantalla()
    {
        InitializeComponent();
        Preparar(AvisoPantalla);
        ListaUniversidades.ItemTemplate = new DataTemplate(CrearTarjeta);
        BotonImportar.Clicked += (_, _) =>
        {
            if (controlador != null)
            {
                Ejecutar(controlador.Importar);
            }
        };
        BotonVerGuardadas.Clicked += (_, _) => Listar();
        CampoPais.Entrada.Completed += (_, _) => Listar();
    }

    internal void EstablecerControlador(UniversidadControlador universidadControlador)
    {
        controlador = universidadControlador;
    }

    private void Listar()
    {
        if (controlador != null)
        {
            Ejecutar(controlador.Listar);
        }
    }

    // ------------------------------------------------------------------ UniversidadVista

    public string? LeerPais()
    {
        return Hilos.EnPrincipalYEsperar(() => CampoPais.Texto);
    }

    public void MostrarResumen(ResumenImportacion resumen)
    {
        Hilos.EnPrincipal(() => MostrarMensaje("Importación de universidades finalizada: " + resumen.Recibidos
            + " recibidas, " + resumen.Registrados + " registradas y " + resumen.Actualizados + " actualizadas"));
    }

    public void MostrarUniversidades(List<Universidad> universidades)
    {
        Hilos.EnPrincipal(() =>
        {
            List<Universidad> lista = universidades ?? [];
            ListaUniversidades.ItemsSource = lista;
            TotalUniversidades.Text = Formato.Cantidad(lista.Count, "universidad", "universidades");
        });
    }

    /// <summary>Tarjeta con nombre, dominio y sitio web (la tabla de los otros clientes).</summary>
    private static object CrearTarjeta()
    {
        var nombre = new Label
        {
            BackgroundColor = Colores.De("AzulClaro"),
            TextColor = Colores.De("Azul"),
            FontSize = 16,
            FontFamily = "sans-serif-medium",
            Padding = new Thickness(14, 10)
        };
        nombre.SetBinding(Label.TextProperty, static (Universidad u) => u.Nombre);
        var dominio = new Label { TextColor = Colores.De("Texto"), FontSize = 14 };
        dominio.SetBinding(Label.TextProperty, static (Universidad u) => u.Dominio,
            converter: new FuncConverter<string?, string>(d => "Dominio: " + (d ?? "—")));
        var sitio = new Label { TextColor = Colores.De("Azul"), FontSize = 14, Margin = new Thickness(0, 4, 0, 0) };
        sitio.SetBinding(Label.TextProperty, static (Universidad u) => u.SitioWeb,
            converter: new FuncConverter<string?, string>(s => s ?? "—"));
        return new Border
        {
            BackgroundColor = Colores.De("Blanco"),
            Stroke = Colores.De("Borde"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Margin = new Thickness(16, 0, 16, 10),
            Content = new VerticalStackLayout
            {
                Children = { nombre, new VerticalStackLayout { Padding = new Thickness(14, 10, 14, 12), Children = { dominio, sitio } } }
            }
        };
    }

    /// <summary>Convertidor de un solo sentido a partir de una función.</summary>
    private sealed class FuncConverter<TOrigen, TDestino>(Func<TOrigen, TDestino> convertir) : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            return convertir((TOrigen)value!);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
