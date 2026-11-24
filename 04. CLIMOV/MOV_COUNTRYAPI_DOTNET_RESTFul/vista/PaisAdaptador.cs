using System.Collections.ObjectModel;
using System.ComponentModel;
using ec.edu.monster.modelos;
using Microsoft.Maui.Controls.Shapes;

namespace ec.edu.monster.vista;

/// <summary>
/// Lista de países en tarjetas (la tabla de los otros clientes). Se usa en Países, Buscar y
/// Consultar en la API; en este último cada tarjeta muestra «Guardado» y el botón Guardar.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
internal sealed class PaisAdaptador
{
    private readonly bool modoConsulta;
    private readonly ObservableCollection<FilaPais> filas = [];
    private FilaPais? seleccionada;

    /// <summary>Al tocar una tarjeta (null: la lista solo se lee, como en «Buscar y filtrar»).</summary>
    public Action<Pais, View>? AlTocar { get; set; }

    /// <summary>Botón «Guardar» de cada tarjeta (solo en «Consultar país en la API»).</summary>
    public Action<Pais>? AlGuardar { get; set; }

    /// <param name="lista">la CollectionView que muestra las tarjetas</param>
    /// <param name="modoConsulta">true en «Consultar país en la API»</param>
    public PaisAdaptador(CollectionView lista, bool modoConsulta)
    {
        this.modoConsulta = modoConsulta;
        lista.SelectionMode = SelectionMode.None;
        lista.ItemTemplate = new DataTemplate(CrearTarjeta);
        lista.ItemsSource = filas;
    }

    public void SetPaises(List<Pais>? nuevos)
    {
        Pais? anterior = seleccionada?.Pais;
        filas.Clear();
        seleccionada = null;
        foreach (Pais pais in nuevos ?? [])
        {
            var fila = new FilaPais(pais, modoConsulta);
            if (pais == anterior)
            {
                fila.Elegido = true;
                seleccionada = fila;
            }
            filas.Add(fila);
        }
    }

    public void Seleccionar(Pais? pais)
    {
        if (seleccionada != null)
        {
            seleccionada.Elegido = false;
        }
        seleccionada = pais == null ? null : filas.FirstOrDefault(f => f.Pais == pais);
        if (seleccionada != null)
        {
            seleccionada.Elegido = true;
        }
    }

    public Pais? Seleccionado => seleccionada?.Pais;

    private object CrearTarjeta()
    {
        var nombre = new Label { FontSize = 17, FontFamily = "sans-serif-medium", TextColor = Colores.De("Azul"), VerticalOptions = LayoutOptions.Center };
        nombre.SetBinding(Label.TextProperty, static (FilaPais f) => f.Nombre);

        var guardado = new Label { FontSize = 12, FontFamily = "sans-serif-medium", VerticalOptions = LayoutOptions.Center };
        guardado.SetBinding(Label.TextProperty, static (FilaPais f) => f.TextoGuardado);
        guardado.SetBinding(Label.TextColorProperty, static (FilaPais f) => f.ColorGuardado);
        var pastilla = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(10, 3),
            Margin = new Thickness(8, 0, 0, 0),
            Content = guardado,
            IsVisible = modoConsulta
        };
        pastilla.SetBinding(VisualElement.BackgroundColorProperty, static (FilaPais f) => f.FondoGuardado);

        var cabecera = new Grid
        {
            BackgroundColor = Colores.De("AzulClaro"),
            Padding = new Thickness(14, 10),
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)]
        };
        cabecera.Add(nombre, 0);
        cabecera.Add(pastilla, 1);

        var ubicacion = Texto(15, "Texto");
        ubicacion.SetBinding(Label.TextProperty, static (FilaPais f) => f.Ubicacion);
        var poblacion = Texto(15, "Texto", true);
        poblacion.SetBinding(Label.TextProperty, static (FilaPais f) => f.Poblacion);
        var area = Texto(15, "Texto", true);
        area.SetBinding(Label.TextProperty, static (FilaPais f) => f.Area);
        var moneda = Texto(15, "Texto");
        moneda.SetBinding(Label.TextProperty, static (FilaPais f) => f.Moneda);

        var cifras = new Grid { ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)], Margin = new Thickness(0, 10, 0, 0) };
        cifras.Add(new VerticalStackLayout { Children = { Texto(12, "TextoSuave", texto: "Población"), poblacion } }, 0);
        cifras.Add(new VerticalStackLayout { Children = { Texto(12, "TextoSuave", texto: "Área (km²)"), area } }, 1);

        var cuerpo = new VerticalStackLayout
        {
            Padding = new Thickness(14),
            Children = { ubicacion, cifras, Texto(12, "TextoSuave", texto: "Moneda", margenArriba: 8), moneda }
        };
        if (modoConsulta)
        {
            var guardar = new Button
            {
                Text = "Guardar",
                ImageSource = "ic_guardar_azul.png",
                ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 8),
                BackgroundColor = Colores.De("AzulClaro"),
                TextColor = Colores.De("Azul"),
                FontFamily = "sans-serif-medium",
                CornerRadius = 20,
                HeightRequest = 40,
                Padding = new Thickness(16, 0),
                HorizontalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 8, 0, 0)
            };
            guardar.Clicked += (origen, _) =>
            {
                if (((BindableObject)origen!).BindingContext is FilaPais fila)
                {
                    AlGuardar?.Invoke(fila.Pais);
                }
            };
            cuerpo.Add(guardar);
        }

        var tarjeta = new Border
        {
            BackgroundColor = Colores.De("Blanco"),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Margin = new Thickness(16, 0, 16, 10),
            Content = new VerticalStackLayout { Children = { cabecera, cuerpo } }
        };
        tarjeta.SetBinding(Border.StrokeProperty, static (FilaPais f) => f.ColorBorde);
        tarjeta.SetBinding(Border.StrokeThicknessProperty, static (FilaPais f) => f.GrosorBorde);
        if (AlTocar != null || modoConsulta)
        {
            var toque = new TapGestureRecognizer();
            toque.Tapped += (origen, _) =>
            {
                if (((BindableObject)origen!).BindingContext is FilaPais fila)
                {
                    AlTocar?.Invoke(fila.Pais, (View)origen);
                }
            };
            tarjeta.GestureRecognizers.Add(toque);
        }
        return tarjeta;
    }

    private static Label Texto(double tamanio, string color, bool resaltado = false, string? texto = null, double margenArriba = 0)
    {
        return new Label
        {
            FontSize = tamanio,
            TextColor = Colores.De(color),
            FontFamily = resaltado ? "sans-serif-medium" : null,
            Text = texto,
            Margin = new Thickness(0, margenArriba, 0, 0)
        };
    }

    /// <summary>Un país ya formateado para su tarjeta; avisa cuando cambia si está elegido.</summary>
    internal sealed class FilaPais(Pais pais, bool modoConsulta) : INotifyPropertyChanged
    {
        private bool elegido;

        public event PropertyChangedEventHandler? PropertyChanged;

        public Pais Pais { get; } = pais;
        public string Nombre { get; } = Formato.Texto(pais.Nombre);
        public string Ubicacion { get; } = ArmarUbicacion(pais);
        public string Poblacion { get; } = Formato.Entero(pais.Poblacion);
        public string Area { get; } = Formato.Decimal(pais.Area);
        public string Moneda { get; } = pais.Moneda ?? "—";
        public string TextoGuardado { get; } = modoConsulta ? (pais.Guardado ? "Guardado: Sí" : "Guardado: No") : "";
        public Color ColorGuardado { get; } = Colores.De(pais.Guardado ? "VerdeTexto" : "TextoSuave");
        public Color FondoGuardado { get; } = Colores.De(pais.Guardado ? "VerdeClaro" : "Fondo");

        public bool Elegido
        {
            get => elegido;
            set
            {
                elegido = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorBorde)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GrosorBorde)));
            }
        }

        public Color ColorBorde => Colores.De(elegido ? "Azul" : "Borde");
        public double GrosorBorde => elegido ? 2 : 1;

        private static string ArmarUbicacion(Pais pais)
        {
            string texto = Formato.Texto(pais.Capital);
            foreach (string? parte in new[] { pais.Region, pais.SubRegion })
            {
                if (!string.IsNullOrEmpty(parte))
                {
                    texto += (texto.Length == 0 ? "" : " · ") + parte;
                }
            }
            return texto;
        }
    }
}
