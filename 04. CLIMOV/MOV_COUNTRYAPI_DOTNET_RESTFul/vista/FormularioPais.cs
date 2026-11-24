using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Formulario de un país (registrar o editar), como diálogo. Entrega el país escrito o null si
/// el usuario cancela. No valida: eso lo hace el controlador con ValidadorDatos, y si hay error
/// vuelve a abrir el formulario con lo escrito y el mensaje arriba.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
internal static class FormularioPais
{
    private static readonly Keyboard PALABRAS = Keyboard.Create(KeyboardFlags.CapitalizeWord);

    /// <param name="principal">página donde se abre el diálogo</param>
    /// <param name="inicial">datos para llenar los campos, o null (país nuevo)</param>
    /// <param name="nuevo">true para el título «Nuevo país»</param>
    /// <param name="error">mensaje de la validación anterior, o null</param>
    /// <param name="respuesta">recibe el país escrito, o null si se cancela</param>
    public static void Mostrar(PaginaPrincipal principal, Pais? inicial, bool nuevo, string? error, Action<Pais?> respuesta)
    {
        var aviso = new Aviso { Margin = new Thickness(0, 0, 0, 4) };
        var nombre = new Campo { Etiqueta = "Nombre *", Teclado = PALABRAS };
        var capital = new Campo { Etiqueta = "Capital", Teclado = PALABRAS };
        var region = new Picker { Title = "Región *", ItemsSource = Pais.REGIONES.ToList() };
        var subregion = new Campo { Etiqueta = "Subregión", Teclado = PALABRAS };
        var poblacion = new Campo { Etiqueta = "Población *", Teclado = Keyboard.Numeric };
        var area = new Campo { Etiqueta = "Área (km²)", Teclado = Keyboard.Numeric };
        var moneda = new Campo { Etiqueta = "Moneda" };

        if (inicial != null)
        {
            nombre.Texto = Formato.Texto(inicial.Nombre);
            capital.Texto = Formato.Texto(inicial.Capital);
            if (inicial.Region != null && Pais.REGIONES.Contains(inicial.Region))
            {
                region.SelectedItem = inicial.Region;
            }
            subregion.Texto = Formato.Texto(inicial.SubRegion);
            // Un valor inválido escrito antes (-1 / NaN) vuelve como campo vacío para corregirlo.
            poblacion.Texto = inicial.Poblacion < 0 ? "" : inicial.Poblacion.ToString();
            area.Texto = double.IsNaN(inicial.Area) || inicial.Area < 0 ? "" : Formato.Editable(inicial.Area);
            moneda.Texto = Formato.Texto(inicial.Moneda);
        }
        if (error != null)
        {
            aviso.Mostrar(error, true);
        }

        var contenido = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                aviso, nombre, capital,
                new VerticalStackLayout
                {
                    Children =
                    {
                        new Label { Text = "Región *", FontSize = 13, TextColor = Colores.De("TextoSuave"), Margin = new Thickness(2, 0, 0, 4) },
                        new Border
                        {
                            Stroke = Colores.De("Borde"), StrokeThickness = 1, BackgroundColor = Colores.De("Blanco"),
                            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                            Padding = new Thickness(12, 2), MinimumHeightRequest = 52, Content = region
                        }
                    }
                },
                subregion, poblacion, area, moneda
            }
        };

        principal.MostrarDialogo(nuevo ? "Nuevo país" : "Datos del país", null, contenido, "Guardar", "Cancelar",
            () =>
            {
                respuesta(new Pais(
                    nombre.Texto.Trim(),
                    Formato.VacioANulo(capital.Texto),
                    Formato.VacioANulo(region.SelectedItem as string),
                    Formato.VacioANulo(subregion.Texto),
                    ValidadorDatos.LeerPoblacion(poblacion.Texto),
                    ValidadorDatos.LeerArea(area.Texto),
                    Formato.VacioANulo(moneda.Texto)));
                return true;
            },
            // Cerrar con «atrás», tocando fuera o con Cancelar.
            () => respuesta(null));
    }
}
