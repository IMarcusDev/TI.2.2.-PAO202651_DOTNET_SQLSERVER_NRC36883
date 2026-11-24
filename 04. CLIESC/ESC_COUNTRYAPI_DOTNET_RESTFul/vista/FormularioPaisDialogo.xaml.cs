using System.Globalization;
using System.Windows;
using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Formulario modal con los siete datos del país (ECUD4: Nombre, Capital, Región como lista
/// desplegable, Subregión, Población, Área (km²) y Moneda). Devuelve lo escrito sin validar;
/// la validación la hace el controlador con ValidadorDatos.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class FormularioPaisDialogo : Window
{
    private const string ELIJA_REGION = "Seleccione...";

    private Pais? resultado;

    private FormularioPaisDialogo(Pais? inicial)
    {
        InitializeComponent();
        Title = inicial == null || inicial.Id == 0 && inicial.Nombre == null ? "Nuevo país" : "Datos del país";
        Region.Items.Add(ELIJA_REGION);
        foreach (string region in Pais.REGIONES)
        {
            Region.Items.Add(region);
        }
        Region.SelectedIndex = 0;
        Llenar(inicial);
        Loaded += (_, _) => Nombre.Focus();
    }

    /// <summary>
    /// Abre el formulario y espera a que el usuario lo cierre.
    /// </summary>
    /// <returns>el país escrito, o null si canceló</returns>
    public static Pais? Pedir(Window? duenio, Pais? inicial)
    {
        var dialogo = new FormularioPaisDialogo(inicial) { Owner = duenio };
        dialogo.ShowDialog();
        return dialogo.resultado;
    }

    private void Llenar(Pais? p)
    {
        if (p == null)
        {
            return;
        }
        Nombre.Text = p.Nombre ?? "";
        Capital.Text = p.Capital ?? "";
        Region.SelectedItem = p.Region != null && Pais.REGIONES.Contains(p.Region) ? p.Region : ELIJA_REGION;
        Subregion.Text = p.SubRegion ?? "";
        // Un valor inválido escrito antes (-1 / NaN) vuelve como campo vacío para corregirlo.
        Poblacion.Text = p.Poblacion < 0 ? "" : p.Poblacion.ToString(CultureInfo.InvariantCulture);
        Area.Text = double.IsNaN(p.Area) || p.Area < 0 ? "" : Estilos.NumeroEditable(p.Area);
        Moneda.Text = p.Moneda ?? "";
    }

    private void AlGuardar(object sender, RoutedEventArgs e)
    {
        string? regionElegida = Region.SelectedItem as string;
        resultado = new Pais(
            Nombre.Text.Trim(),
            VacioANulo(Capital.Text),
            regionElegida == ELIJA_REGION ? null : regionElegida,
            VacioANulo(Subregion.Text),
            ValidadorDatos.LeerPoblacion(Poblacion.Text),
            ValidadorDatos.LeerArea(Area.Text),
            VacioANulo(Moneda.Text));
        Close();
    }

    private static string? VacioANulo(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
