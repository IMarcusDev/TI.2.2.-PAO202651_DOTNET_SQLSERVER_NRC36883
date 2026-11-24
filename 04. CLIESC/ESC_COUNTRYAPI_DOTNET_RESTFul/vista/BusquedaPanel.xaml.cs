using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// «Buscar y filtrar» (ECUD5): nombre y capital por coincidencia parcial, y filtros de región
/// (Todas, América, Europa, ...) que vuelven a consultar en cuanto se elige uno.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class BusquedaPanel : PanelBase, BusquedaVista
{
    private const string TODAS = "Todas";

    private readonly List<RadioButton> botonesRegion = [];
    private Action alCambiarRegion = () => { };

    public BusquedaPanel(VentanaPrincipal ventana)
    {
        InitializeComponent();
        Preparar(ventana, AlertaPantalla);
    }

    public void EstablecerControlador(PaisControlador controlador)
    {
        BotonBuscar.Click += (_, _) => Ejecutar(controlador.BuscarYFiltrar);
        KeyEventHandler alEnter = (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Ejecutar(controlador.BuscarYFiltrar);
            }
        };
        Nombre.KeyDown += alEnter;
        Capital.KeyDown += alEnter;
        alCambiarRegion = () => Ejecutar(controlador.BuscarYFiltrar);
    }

    public CriteriosBusqueda LeerCriterios()
    {
        string? region = null;
        foreach (RadioButton boton in botonesRegion)
        {
            if (boton.IsChecked == true && (string)boton.Content != TODAS)
            {
                region = (string)boton.Content;
            }
        }
        return new CriteriosBusqueda(Nombre.Text, Capital.Text, region);
    }

    public void MostrarFiltrosRegion(IReadOnlyList<string> regiones)
    {
        Filtros.Children.Clear();
        botonesRegion.Clear();
        Filtros.Children.Add(new TextBlock { Text = "Región:", Style = (Style)FindResource("Etiqueta") });
        var opciones = new List<string> { TODAS };
        opciones.AddRange(regiones);
        foreach (string opcion in opciones)
        {
            RadioButton boton = Chip(opcion);
            botonesRegion.Add(boton);
            Filtros.Children.Add(boton);
        }
        botonesRegion[0].IsChecked = true;
        // Se engancha después de marcar «Todas» para no lanzar una búsqueda extra al abrir.
        // Checked (y no Click) cubre también el teclado y la automatización de la interfaz.
        foreach (RadioButton boton in botonesRegion)
        {
            boton.Checked += (_, _) => alCambiarRegion();
        }
    }

    public void MostrarResultados(List<Pais> paises, int total)
    {
        Tabla.ItemsSource = paises;
        Total.Text = total == 1 ? "1 país encontrado" : total + " países encontrados";
    }

    /// <summary>Botón tipo «chip»: azul cuando está elegido. Al elegirlo se vuelve a buscar.</summary>
    private RadioButton Chip(string texto)
    {
        return new RadioButton
        {
            Content = texto,
            GroupName = "RegionBusqueda",
            Style = (Style)FindResource("Chip")
        };
    }
}
