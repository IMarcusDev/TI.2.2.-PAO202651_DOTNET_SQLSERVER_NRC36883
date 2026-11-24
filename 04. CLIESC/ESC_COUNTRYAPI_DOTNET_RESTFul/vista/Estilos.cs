using System.Globalization;
using System.Windows.Data;

namespace ec.edu.monster.vista;

/// <summary>
/// Formatos de números compartidos por las pantallas WPF (la paleta y los estilos están en
/// Estilos.xaml). Equivale a la parte de números de Estilos.java del cliente Swing.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public static class Estilos
{
    public const string TITULO_APLICACION = "CountryAPI";

    private static readonly CultureInfo ECUADOR = CultureInfo.GetCultureInfo("es-EC");

    /// <summary>Entero con separador de miles (RNF-009), por ejemplo 17.643.060.</summary>
    public static string FormatearEntero(long valor)
    {
        return valor.ToString("#,##0", ECUADOR);
    }

    /// <summary>Número con separador de miles y hasta dos decimales.</summary>
    public static string FormatearDecimal(double valor)
    {
        return valor.ToString("#,##0.##", ECUADOR);
    }

    /// <summary>Número sin separadores para llenar un campo editable (276841.5 → «276841,5»).</summary>
    public static string NumeroEditable(double valor)
    {
        if (valor == Math.Round(valor))
        {
            return ((long)valor).ToString(CultureInfo.InvariantCulture);
        }
        return valor.ToString("R", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}

/// <summary>Muestra los números de las tablas con separador de miles (enteros) o dos decimales.</summary>
public sealed class FormatoNumero : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            double d => Estilos.FormatearDecimal(d),
            float f => Estilos.FormatearDecimal(f),
            long l => Estilos.FormatearEntero(l),
            int i => Estilos.FormatearEntero(i),
            null => "",
            _ => value.ToString() ?? ""
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>Columna «¿Guardado?» de la consulta a la API: «Sí» o «No».</summary>
public sealed class SiNo : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Sí" : "No";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
