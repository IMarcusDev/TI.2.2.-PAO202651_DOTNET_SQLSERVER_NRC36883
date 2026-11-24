using System.Globalization;

namespace ec.edu.monster.vista;

/// <summary>
/// Números de las páginas con el formato de Ecuador (el fmt:formatNumber con es_EC de las JSP):
/// separador de miles «.» y decimal «,». Los separadores se fijan aquí para no depender de la
/// cultura del sistema.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public static class Formato
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

    /// <summary>Número para un campo editable: sin separador de miles y hasta dos decimales (276841,5).</summary>
    public static string Editable(double valor)
    {
        return valor.ToString("0.##", ECUADOR);
    }
}
