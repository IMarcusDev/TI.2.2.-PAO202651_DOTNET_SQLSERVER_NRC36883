using System.Globalization;
using System.Text;

namespace ec.edu.monster.vista;

/// <summary>
/// Tabla de texto con bordes, como la DataGrid de escritorio:
/// <code>
/// +----+---------+------------+
/// | ID | Nombre  | Población  |
/// +----+---------+------------+
/// |  1 | Ecuador | 17.643.060 |
/// +----+---------+------------+
/// </code>
/// Las columnas numéricas van a la derecha. Un texto más largo que el ancho máximo de su
/// columna se corta con «…» para que la tabla no se desborde.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Tabla
{
    /// <summary>Separadores de Ecuador fijados aquí para no depender de los datos de cultura del sistema.</summary>
    private static readonly NumberFormatInfo ECUADOR = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NegativeSign = "-"
    };

    private readonly string[] encabezados;
    private readonly int[] maximos;
    private readonly bool[] derecha;
    private readonly List<string[]> filas = [];

    public Tabla(params string[] encabezados)
    {
        this.encabezados = encabezados;
        maximos = Enumerable.Repeat(int.MaxValue, encabezados.Length).ToArray();
        derecha = new bool[encabezados.Length];
    }

    /// <summary>Alinea a la derecha las columnas indicadas (números).</summary>
    public Tabla Derecha(params int[] columnas)
    {
        foreach (int columna in columnas)
        {
            derecha[columna] = true;
        }
        return this;
    }

    /// <summary>Ancho máximo de una columna.</summary>
    public Tabla Maximo(int columna, int ancho)
    {
        maximos[columna] = Math.Max(3, ancho);
        return this;
    }

    public Tabla Fila(params object?[] celdas)
    {
        var texto = new string[encabezados.Length];
        for (int i = 0; i < texto.Length; i++)
        {
            object? celda = i < celdas.Length ? celdas[i] : null;
            texto[i] = celda?.ToString() ?? "";
        }
        filas.Add(texto);
        return this;
    }

    public int CantidadFilas => filas.Count;

    /// <summary>La tabla completa, con salto de línea al final.</summary>
    public string Texto()
    {
        var anchos = new int[encabezados.Length];
        for (int i = 0; i < anchos.Length; i++)
        {
            anchos[i] = Math.Min(maximos[i], encabezados[i].Length);
            foreach (string[] fila in filas)
            {
                anchos[i] = Math.Max(anchos[i], Math.Min(maximos[i], fila[i].Length));
            }
        }
        var texto = new StringBuilder();
        string borde = Borde(anchos);
        texto.Append(borde).Append(Linea(encabezados, anchos, new bool[anchos.Length])).Append(borde);
        foreach (string[] fila in filas)
        {
            texto.Append(Linea(fila, anchos, derecha));
        }
        if (filas.Count > 0)
        {
            texto.Append(borde);
        }
        return texto.ToString();
    }

    /// <summary>Entero con separador de miles de Ecuador, por ejemplo 17.643.060.</summary>
    public static string Entero(long valor)
    {
        return valor.ToString("#,##0", ECUADOR);
    }

    /// <summary>Número con separador de miles y hasta dos decimales.</summary>
    public static string Decimal(double valor)
    {
        return valor.ToString("#,##0.##", ECUADOR);
    }

    private static string Borde(int[] anchos)
    {
        var linea = new StringBuilder("+");
        foreach (int ancho in anchos)
        {
            linea.Append('-', ancho + 2).Append('+');
        }
        return linea.Append(Environment.NewLine).ToString();
    }

    private static string Linea(string[] celdas, int[] anchos, bool[] derecha)
    {
        var linea = new StringBuilder("|");
        for (int i = 0; i < anchos.Length; i++)
        {
            string celda = Cortar(celdas[i], anchos[i]);
            string relleno = new(' ', anchos[i] - celda.Length);
            linea.Append(' ').Append(derecha[i] ? relleno + celda : celda + relleno).Append(" |");
        }
        return linea.Append(Environment.NewLine).ToString();
    }

    private static string Cortar(string texto, int ancho)
    {
        return texto.Length <= ancho ? texto : texto[..(ancho - 1)] + "…";
    }
}
