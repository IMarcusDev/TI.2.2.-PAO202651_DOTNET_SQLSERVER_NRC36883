using System.Text;

namespace ec.edu.monster.vista;

/// <summary>
/// Entrada y salida de texto de todas las pantallas: títulos, mensajes, errores con el prefijo
/// «ERROR:», tablas y lectura de datos. Recibe la entrada y la salida por constructor para que
/// las pruebas puedan escribir las respuestas del usuario y leer lo que se mostró.
///
/// Los colores del diseño de referencia (azul para títulos, verde para mensajes y rojo para
/// errores) se usan solo en terminales que entienden ANSI; el texto es el mismo con o sin color.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Consola
{
    /// <summary>El usuario eligió salir o la entrada se terminó (fin de archivo).</summary>
    public sealed class Salida : Exception
    {
        public Salida() : base("Fin de la entrada")
        {
        }
    }

    internal const int ANCHO = 100;
    private const string AZUL = "\u001B[1;34m";
    private const string VERDE = "\u001B[32m";
    private const string ROJO = "\u001B[1;31m";
    private const string TENUE = "\u001B[2m";
    private const string NORMAL = "\u001B[0m";

    private readonly TextReader entrada;
    private readonly TextWriter salida;
    private readonly bool colores;
    /// <summary>True si la entrada es un teclado real: la clave se lee sin mostrarla.</summary>
    private readonly bool terminal;

    /// <summary>Consola de pruebas (o de cualquier flujo): sin colores y sin ocultar la clave.</summary>
    public Consola(TextReader entrada, TextWriter salida) : this(entrada, salida, false, false)
    {
    }

    private Consola(TextReader entrada, TextWriter salida, bool colores, bool terminal)
    {
        this.entrada = entrada;
        this.salida = salida;
        this.colores = colores;
        this.terminal = terminal;
    }

    /// <summary>
    /// Consola del sistema en UTF-8 (las tildes de los países y mensajes se ven bien también en
    /// Windows) y con colores si la terminal los soporta.
    /// </summary>
    public static Consola DelSistema()
    {
        var utf8 = new UTF8Encoding(false);
        try
        {
            Console.OutputEncoding = utf8;
            Console.InputEncoding = utf8;
        }
        catch (IOException)
        {
            // Sin consola real (salida redirigida) no hace falta cambiar la página de códigos.
        }
        bool terminal = !Console.IsInputRedirected && !Console.IsOutputRedirected;
        // Windows Terminal (WT_SESSION), Git Bash y las terminales tipo Unix (TERM) entienden
        // ANSI; la consola clásica de Windows (cmd sin Windows Terminal) no siempre.
        // La variable NO_COLOR los desactiva.
        bool colores = terminal && Environment.GetEnvironmentVariable("NO_COLOR") == null
            && (Environment.GetEnvironmentVariable("WT_SESSION") != null
                || Environment.GetEnvironmentVariable("TERM") != null
                || Environment.GetEnvironmentVariable("TERM_PROGRAM") != null);
        var salida = new StreamWriter(Console.OpenStandardOutput(), utf8) { AutoFlush = true };
        var entrada = new StreamReader(Console.OpenStandardInput(), utf8);
        return new Consola(entrada, salida, colores, terminal);
    }

    // ------------------------------------------------------------------ salida

    /// <summary>Título de pantalla entre líneas, como la barra azul de los otros clientes.</summary>
    public void Titulo(string texto)
    {
        salida.WriteLine();
        salida.WriteLine(Color(AZUL, new string('=', ANCHO)));
        salida.WriteLine(Color(AZUL, " " + texto));
        salida.WriteLine(Color(AZUL, new string('=', ANCHO)));
    }

    /// <summary>Subtítulo de una sección dentro de la pantalla.</summary>
    public void Subtitulo(string texto)
    {
        salida.WriteLine();
        salida.WriteLine(Color(AZUL, texto));
        salida.WriteLine(Color(AZUL, new string('-', texto.Length)));
    }

    /// <summary>Mensaje de éxito (verde).</summary>
    public void Mensaje(string? texto)
    {
        salida.WriteLine(Color(VERDE, texto ?? ""));
    }

    /// <summary>Error (rojo), siempre con el prefijo «ERROR:».</summary>
    public void Error(string? texto)
    {
        salida.WriteLine(Color(ROJO, "ERROR: " + texto));
    }

    /// <summary>Texto normal.</summary>
    public void Texto(string texto)
    {
        salida.WriteLine(texto);
    }

    /// <summary>Texto secundario (ayudas, «Procesando...»).</summary>
    public void Ayuda(string texto)
    {
        salida.WriteLine(Color(TENUE, texto));
    }

    public void LineaEnBlanco()
    {
        salida.WriteLine();
    }

    public void MostrarTabla(Tabla tabla)
    {
        salida.Write(tabla.Texto());
    }

    // ------------------------------------------------------------------ entrada

    /// <summary>Lee una línea (sin espacios a los lados). Si la entrada se termina, sale del programa.</summary>
    public string Leer(string etiqueta)
    {
        salida.Write(etiqueta + ": ");
        salida.Flush();
        string? linea;
        try
        {
            linea = entrada.ReadLine();
        }
        catch (IOException)
        {
            throw new Salida();
        }
        if (linea == null)
        {
            salida.WriteLine();
            throw new Salida();
        }
        return linea.Trim();
    }

    /// <summary>Lee la clave sin mostrarla cuando hay una terminal real.</summary>
    public string LeerClave(string etiqueta)
    {
        if (!terminal)
        {
            return Leer(etiqueta);
        }
        salida.Write(etiqueta + ": ");
        salida.Flush();
        var clave = new StringBuilder();
        while (true)
        {
            ConsoleKeyInfo tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter)
            {
                break;
            }
            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (clave.Length > 0)
                {
                    clave.Length--;
                }
            }
            else if ((tecla.Modifiers & ConsoleModifiers.Control) != 0 && tecla.Key is ConsoleKey.Z or ConsoleKey.D or ConsoleKey.C)
            {
                // Ctrl+Z / Ctrl+D: fin de la entrada, como en el resto de preguntas.
                salida.WriteLine();
                throw new Salida();
            }
            else if (!char.IsControl(tecla.KeyChar))
            {
                clave.Append(tecla.KeyChar);
            }
        }
        salida.WriteLine();
        return clave.ToString().Trim();
    }

    /// <summary>Pregunta de sí o no: «S», «Si» o «Sí» cuentan como sí.</summary>
    public bool Confirmar(string pregunta)
    {
        string respuesta = Leer(pregunta + " (S/N)").ToLowerInvariant();
        return respuesta is "s" or "si" or "sí";
    }

    /// <summary>Lee un número de opción; devuelve -1 si no es un número.</summary>
    public int LeerOpcion(string etiqueta)
    {
        return int.TryParse(Leer(etiqueta), out int opcion) ? opcion : -1;
    }

    private string Color(string codigo, string texto)
    {
        return colores ? codigo + texto + NORMAL : texto;
    }
}
