using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ec.edu.monster.config;

/// <summary>
/// Dirección del servidor RESTful y tiempo de espera de las peticiones.
///
/// La URL se lee de appsettings.json (clave Servidor:Url) para poder cambiar la IP sin
/// recompilar. Se busca, en este orden:
/// <list type="number">
///   <item>el archivo indicado en la variable de entorno COUNTRYAPI_CONFIG;</item>
///   <item>appsettings.json en la carpeta de trabajo;</item>
///   <item>appsettings.json junto al ejecutable (se copia al compilar).</item>
/// </list>
/// Si no se encuentra, se usa http://localhost:5088/api.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public static class ConfigServidor
{
    public const string ARCHIVO = "appsettings.json";
    public const string URL_POR_DEFECTO = "http://localhost:5088/api";

    /// <summary>Tiempo máximo de espera de cada petición al servidor, en segundos.</summary>
    public const int TIMEOUT_SEGUNDOS = 15;

    /// <summary>URL base del servidor, sin «/» al final.</summary>
    public static readonly string URL_BASE = QuitarBarraFinal(Cargar() ?? URL_POR_DEFECTO);

    private static string? Cargar()
    {
        string? ruta = Environment.GetEnvironmentVariable("COUNTRYAPI_CONFIG");
        string[] candidatos = string.IsNullOrWhiteSpace(ruta)
            ? [Path.Combine(Environment.CurrentDirectory, ARCHIVO), Path.Combine(AppContext.BaseDirectory, ARCHIVO)]
            : [ruta.Trim()];
        foreach (string archivo in candidatos)
        {
            try
            {
                if (File.Exists(archivo))
                {
                    string? url = JObject.Parse(File.ReadAllText(archivo))["Servidor"]?["Url"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return url.Trim();
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning("No se pudo leer " + archivo + "; se usa la dirección por defecto: " + e.Message);
            }
        }
        return null;
    }

    private static string QuitarBarraFinal(string url)
    {
        return url.EndsWith('/') ? url[..^1] : url;
    }
}
