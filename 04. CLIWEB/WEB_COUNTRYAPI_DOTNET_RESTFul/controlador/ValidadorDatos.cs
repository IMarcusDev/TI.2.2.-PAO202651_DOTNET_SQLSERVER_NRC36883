using System.Globalization;
using System.Text.RegularExpressions;
using ec.edu.monster.modelos;

namespace ec.edu.monster.controlador;

/// <summary>
/// Validaciones de los datos que escribe el usuario, iguales a las del ECUD. Cada método devuelve
/// el mensaje de error que se debe mostrar, o null si los datos son válidos; así el cliente no
/// envía peticiones con datos incompletos.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public static class ValidadorDatos
{
    public const string INGRESE_USUARIO = "Ingrese el usuario";
    public const string INGRESE_CLAVE = "Ingrese la clave";
    public const string NOMBRE_CORTO = "Ingrese al menos dos caracteres";
    public const string NOMBRE_PAIS = "Ingrese el nombre del país";
    public const string SELECCIONE_REGION = "Seleccione una región";
    public const string POBLACION = "La población debe ser un número entero mayor o igual que cero";
    public const string AREA = "El área debe ser un número mayor o igual que cero";
    public const string TEXTOS_LARGOS = "La capital, la subregión y la moneda deben tener como máximo 100 caracteres";
    public const string INGRESE_PAIS = "Ingrese el país";

    private const int LARGO_MAXIMO = 100;

    // ECMAScript: \d y \s solo aceptan dígitos y espacios ASCII, como en Java.
    /// <summary>Entero con separadores de miles («17.643.060», «17,643,060» o «17 643 060»).</summary>
    private static readonly Regex ENTERO_CON_MILES = new(@"^\d{1,3}([.,\s]\d{3})+$", RegexOptions.ECMAScript);
    private static readonly Regex ENTERO = new(@"^\d+$", RegexOptions.ECMAScript);
    /// <summary>Número con una parte decimal opcional, con punto o coma («276841», «2,5»).</summary>
    private static readonly Regex DECIMAL = new(@"^\d+([.,]\d+)?$", RegexOptions.ECMAScript);
    private static readonly Regex SEPARADORES = new(@"[.,\s]", RegexOptions.ECMAScript);

    /// <summary>Usuario y clave no vacíos (ECUD1, flujo alternativo 2).</summary>
    public static string? ValidarCredenciales(string? usuario, string? clave)
    {
        if (string.IsNullOrWhiteSpace(usuario))
        {
            return INGRESE_USUARIO;
        }
        if (string.IsNullOrEmpty(clave))
        {
            return INGRESE_CLAVE;
        }
        return null;
    }

    /// <summary>Al menos dos caracteres para consultar en la API (ECUD3, flujo alternativo 3).</summary>
    public static string? ValidarNombreConsulta(string? nombre)
    {
        return nombre == null || nombre.Trim().Length < 2 ? NOMBRE_CORTO : null;
    }

    /// <summary>Datos de un país antes de registrarlo o actualizarlo (ECUD4, flujo alternativo 2).</summary>
    public static string? ValidarPais(Pais? pais)
    {
        if (pais == null || string.IsNullOrWhiteSpace(pais.Nombre) || pais.Nombre.Trim().Length > LARGO_MAXIMO)
        {
            return NOMBRE_PAIS;
        }
        if (pais.Region == null || !Pais.REGIONES.Contains(pais.Region))
        {
            return SELECCIONE_REGION;
        }
        if (pais.Poblacion < 0)
        {
            return POBLACION;
        }
        if (pais.Area < 0 || double.IsNaN(pais.Area) || double.IsInfinity(pais.Area))
        {
            return AREA;
        }
        if (MuyLargo(pais.Capital) || MuyLargo(pais.SubRegion) || MuyLargo(pais.Moneda))
        {
            return TEXTOS_LARGOS;
        }
        return null;
    }

    /// <summary>País para importar o listar universidades, no vacío (ECUD10, flujo alternativo 2).</summary>
    public static string? ValidarPaisUniversidades(string? pais)
    {
        return string.IsNullOrWhiteSpace(pais) ? INGRESE_PAIS : null;
    }

    /// <summary>
    /// Convierte el texto de la población. Acepta separadores de miles. Si el texto no es un
    /// entero mayor o igual que cero devuelve -1, que <see cref="ValidarPais"/> rechaza.
    /// </summary>
    public static long LeerPoblacion(string? texto)
    {
        if (texto == null)
        {
            return -1;
        }
        string limpio = texto.Trim();
        if (ENTERO_CON_MILES.IsMatch(limpio))
        {
            limpio = SEPARADORES.Replace(limpio, "");
        }
        if (!ENTERO.IsMatch(limpio))
        {
            return -1;
        }
        return long.TryParse(limpio, NumberStyles.None, CultureInfo.InvariantCulture, out long valor) ? valor : -1;
    }

    /// <summary>
    /// Convierte el texto del área (km²), con punto o coma decimal. Si no es un número mayor o
    /// igual que cero devuelve NaN, que <see cref="ValidarPais"/> rechaza. Vacío equivale a 0.
    /// </summary>
    public static double LeerArea(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return 0;
        }
        string limpio = texto.Trim();
        if (!DECIMAL.IsMatch(limpio))
        {
            return double.NaN;
        }
        return double.Parse(limpio.Replace(',', '.'), CultureInfo.InvariantCulture);
    }

    private static bool MuyLargo(string? texto)
    {
        return texto != null && texto.Trim().Length > LARGO_MAXIMO;
    }
}
