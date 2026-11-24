using System.Globalization;

namespace ec.edu.monster.vista;

/// <summary>
/// Formato de números como en los demás clientes: separador de miles de Ecuador (17.643.060).
/// Los separadores se fijan aquí para no depender de los datos de cultura del dispositivo.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
internal static class Formato
{
    private static readonly NumberFormatInfo ECUADOR = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NegativeSign = "-"
    };

    /// <summary>Entero con separador de miles, por ejemplo 17.643.060.</summary>
    public static string Entero(long valor)
    {
        return valor.ToString("#,##0", ECUADOR);
    }

    /// <summary>Número con separador de miles y hasta dos decimales.</summary>
    public static string Decimal(double valor)
    {
        return valor.ToString("#,##0.##", ECUADOR);
    }

    /// <summary>Número sin separadores para llenar un campo editable (276841.5 → «276841,5»).</summary>
    public static string Editable(double valor)
    {
        if (valor == Math.Round(valor))
        {
            return ((long)valor).ToString(CultureInfo.InvariantCulture);
        }
        return valor.ToString("R", CultureInfo.InvariantCulture).Replace('.', ',');
    }

    /// <summary>Texto o cadena vacía si es null.</summary>
    public static string Texto(string? valor)
    {
        return valor ?? "";
    }

    /// <summary>Texto recortado, o null si está vacío.</summary>
    public static string? VacioANulo(string? valor)
    {
        if (valor == null)
        {
            return null;
        }
        string limpio = valor.Trim();
        return limpio.Length == 0 ? null : limpio;
    }

    /// <summary>«1 país» / «N países» (los plurales de strings.xml de Android).</summary>
    public static string Cantidad(int cantidad, string singular, string plural)
    {
        return cantidad + " " + (cantidad == 1 ? singular : plural);
    }
}
