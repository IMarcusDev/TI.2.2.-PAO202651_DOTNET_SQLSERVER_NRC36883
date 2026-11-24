using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ec.edu.monster.config;

/// <summary>
/// Escribe los double del contrato con el mismo texto que el servidor Java (JSON-B usa
/// Double.toString), para que las respuestas sean idénticas (DUDAS.md, H8):
/// <list type="bullet">
///   <item>entre 0,001 y 10 000 000: notación decimal con al menos un decimal (276841.0, 0.5);</item>
///   <item>fuera de ese rango: notación científica de Java (1.7098242E7, 1.0E-4).</item>
/// </list>
/// Los dígitos son los mínimos que identifican el número, igual en .NET («R») y en Java 19+.
/// Al leer acepta cualquier número JSON.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class DoubleFormatoJava : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDouble();
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        writer.WriteRawValue(ComoJava(value), skipInputValidation: true);
    }

    /// <summary>Texto de Double.toString de Java para ese número.</summary>
    public static string ComoJava(double valor)
    {
        if (double.IsNaN(valor) || double.IsInfinity(valor))
        {
            // JSON no admite NaN ni infinito; el servidor nunca los guarda (CHECK y validación).
            return "0.0";
        }
        if (valor == 0)
        {
            return double.IsNegative(valor) ? "-0.0" : "0.0";
        }
        string signo = valor < 0 ? "-" : "";
        double absoluto = Math.Abs(valor);

        // Dígitos significativos mínimos y posición del punto decimal.
        string r = absoluto.ToString("R", CultureInfo.InvariantCulture);
        int exponente = 0;
        int posE = r.IndexOfAny(['E', 'e']);
        if (posE >= 0)
        {
            exponente = int.Parse(r[(posE + 1)..], CultureInfo.InvariantCulture);
            r = r[..posE];
        }
        int punto = r.IndexOf('.');
        string entera = punto < 0 ? r : r[..punto];
        string fraccion = punto < 0 ? "" : r[(punto + 1)..];
        string digitos = entera + fraccion;
        int posicionPunto = entera.Length + exponente;
        int ceros = 0;
        while (ceros < digitos.Length - 1 && digitos[ceros] == '0')
        {
            ceros++;
        }
        digitos = digitos[ceros..];
        posicionPunto -= ceros;
        digitos = digitos.TrimEnd('0');
        if (digitos.Length == 0)
        {
            digitos = "0";
        }

        if (absoluto >= 1e-3 && absoluto < 1e7)
        {
            if (posicionPunto <= 0)
            {
                return signo + "0." + new string('0', -posicionPunto) + digitos;
            }
            if (posicionPunto >= digitos.Length)
            {
                return signo + digitos + new string('0', posicionPunto - digitos.Length) + ".0";
            }
            return signo + digitos[..posicionPunto] + "." + digitos[posicionPunto..];
        }
        string mantisa = digitos[..1] + "." + (digitos.Length > 1 ? digitos[1..] : "0");
        return signo + mantisa + "E" + (posicionPunto - 1).ToString(CultureInfo.InvariantCulture);
    }
}
