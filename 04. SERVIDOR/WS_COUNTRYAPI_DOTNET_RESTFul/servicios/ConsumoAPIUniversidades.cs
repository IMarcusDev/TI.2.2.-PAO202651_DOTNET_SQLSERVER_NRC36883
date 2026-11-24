using System.Globalization;
using System.Net;
using System.Text;
using ec.edu.monster.modelos.data;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ec.edu.monster.servicios;

/// <summary>
/// Consumo de la API pública de universidades de Hipolabs (RQF-011, ECUD11).
///
/// Es el único lugar del sistema que conoce la dirección de esta API y el formato de su JSON.
/// Hace GET http://universities.hipolabs.com/search?country={pais} con HttpClient, espera como
/// máximo <see cref="TIMEOUT_SEGUNDOS"/> segundos y convierte el arreglo JSON en objetos
/// <see cref="Universidad"/> con Newtonsoft.Json. Nunca toca la base de datos.
///
/// Si la API no responde, devuelve un código distinto de 200 o un JSON no válido, lanza
/// IOException «La API de universidades no está disponible, intente más tarde» (502).
/// Si la API responde una lista vacía, devuelve la lista vacía: el controlador decide el 404.
///
/// La dirección se lee en cada petición de CountryApi:UniversidadesUrl (por ejemplo, para
/// probar la API caída).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ConsumoAPIUniversidades
{
    internal const string NO_DISPONIBLE = "La API de universidades no está disponible, intente más tarde";

    /// <summary>Dirección de la API si la configuración no indica otra.</summary>
    public const string URL_POR_DEFECTO = "http://universities.hipolabs.com";

    public const int TIMEOUT_SEGUNDOS = 15;
    private const int MAXIMO_REDIRECCIONES = 3;

    private readonly HttpClient http;
    private readonly IConfiguration? configuracion;
    private readonly string? urlFija;
    private readonly ILogger log;

    /// <summary>Constructor que usa Program.cs: HttpClient de IHttpClientFactory y la configuración.</summary>
    public ConsumoAPIUniversidades(HttpClient http, IConfiguration configuracion, ILogger<ConsumoAPIUniversidades> log)
    {
        this.http = http;
        this.configuracion = configuracion;
        this.log = log;
    }

    /// <summary>Permite apuntar a otra dirección (pruebas con un servidor falso).</summary>
    internal ConsumoAPIUniversidades(string urlBase)
    {
        http = new HttpClient(CrearManejador()) { Timeout = TimeSpan.FromSeconds(TIMEOUT_SEGUNDOS) };
        urlFija = urlBase;
        log = NullLogger.Instance;
    }

    /// <summary>
    /// Manejador HTTP que sigue hasta 3 redirecciones, como el servidor Java (que las sigue a mano
    /// porque HttpURLConnection no pasa de HTTP a HTTPS).
    /// </summary>
    public static HttpMessageHandler CrearManejador()
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = MAXIMO_REDIRECCIONES
        };
    }

    /// <summary>Dirección base de la API, sin «/» al final; se lee en cada petición.</summary>
    public string URL_BASE
    {
        get
        {
            string url = urlFija ?? configuracion?["CountryApi:UniversidadesUrl"] ?? URL_POR_DEFECTO;
            if (string.IsNullOrWhiteSpace(url))
            {
                url = URL_POR_DEFECTO;
            }
            return url.Trim().TrimEnd('/');
        }
    }

    /// <summary>Universidades del país (nombre del país en inglés, como lo espera la API).</summary>
    public async Task<List<Universidad>> ObtenerPorPais(string pais)
    {
        // WebUtility.UrlEncode codifica el espacio como «+», igual que URLEncoder de Java.
        string url = URL_BASE + "/search?country=" + WebUtility.UrlEncode(pais.Trim());
        return ConvertirRespuesta(await GetJson(url), pais.Trim());
    }

    /// <summary>
    /// Convierte el arreglo JSON en universidades y descarta las repetidas con el mismo nombre
    /// (ECUD11, paso 6).
    /// </summary>
    /// <param name="json">cuerpo de la respuesta</param>
    /// <param name="paisPedido">país de la consulta, por si algún elemento no trae «country»</param>
    internal List<Universidad> ConvertirRespuesta(string json, string paisPedido)
    {
        try
        {
            if (Parsear(json) is not JArray arreglo)
            {
                throw new IOException(NO_DISPONIBLE);
            }
            var universidades = new List<Universidad>();
            var nombres = new HashSet<string>();
            foreach (JToken elemento in arreglo)
            {
                if (elemento is not JObject objeto)
                {
                    throw new IOException(NO_DISPONIBLE);
                }
                Universidad? u = Convertir(objeto);
                if (u == null)
                {
                    continue;
                }
                u.Pais ??= Recortar(paisPedido, 100);
                if (nombres.Add(Normalizar(u.Nombre)))
                {
                    universidades.Add(u);
                }
            }
            return universidades;
        }
        catch (Exception e) when (e is JsonException or InvalidCastException or FormatException or ArgumentException)
        {
            throw new IOException(NO_DISPONIBLE, e);
        }
    }

    /// <summary>
    /// Mapeo de una universidad del JSON de Hipolabs:
    /// <list type="bullet">
    ///   <item>nombre: name (sin nombre, el elemento se descarta);</item>
    ///   <item>dominio: primer elemento de domains; «Sin dominio» si no tiene;</item>
    ///   <item>sitio web: primer elemento de web_pages; «Sin sitio web» si no tiene;</item>
    ///   <item>país: country.</item>
    /// </list>
    /// Los textos se recortan al largo de las columnas de la tabla Universidades.
    /// </summary>
    public Universidad? Convertir(JObject json)
    {
        string? nombre = Texto(json["name"]);
        if (nombre == null)
        {
            return null;
        }
        string? dominio = PrimerTexto(json["domains"]);
        string? sitioWeb = PrimerTexto(json["web_pages"]);
        string? pais = Texto(json["country"]);
        return new Universidad(
            Recortar(nombre, 200),
            Recortar(dominio ?? "Sin dominio", 150),
            Recortar(sitioWeb ?? "Sin sitio web", 250),
            pais == null ? null : Recortar(pais, 100));
    }

    private static string? Texto(JToken? valor)
    {
        if (valor is not JValue simple || simple.Type == JTokenType.Null)
        {
            return null;
        }
        string texto = (simple.Type == JTokenType.Boolean
            ? ((bool)simple ? "true" : "false")
            : Convert.ToString(simple.Value, CultureInfo.InvariantCulture) ?? "").Trim();
        return texto.Length == 0 ? null : texto;
    }

    private static string? PrimerTexto(JToken? lista)
    {
        if (lista is not JArray arreglo)
        {
            return null;
        }
        foreach (JToken e in arreglo)
        {
            string? valor = Texto(e);
            if (valor != null)
            {
                return valor;
            }
        }
        return null;
    }

    private static string Recortar(string texto, int largo)
    {
        return texto.Length <= largo ? texto : texto[..largo];
    }

    /// <summary>Texto sin tildes y en minúsculas, para descartar los nombres repetidos.</summary>
    private static string Normalizar(string texto)
    {
        string descompuesto = texto.Normalize(NormalizationForm.FormD);
        var sinTildes = new StringBuilder(descompuesto.Length);
        foreach (char c in descompuesto)
        {
            UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria != UnicodeCategory.NonSpacingMark
                && categoria != UnicodeCategory.SpacingCombiningMark
                && categoria != UnicodeCategory.EnclosingMark)
            {
                sinTildes.Append(c);
            }
        }
        return sinTildes.ToString().ToLowerInvariant();
    }

    /// <summary>Lee el JSON sin convertir textos en fechas (Newtonsoft lo haría por defecto).</summary>
    private static JToken? Parsear(string json)
    {
        using var lector = new JsonTextReader(new StringReader(json ?? ""))
        {
            DateParseHandling = DateParseHandling.None
        };
        return JToken.ReadFrom(lector);
    }

    /// <summary>GET que devuelve el cuerpo si la respuesta es 200 (las redirecciones las sigue el manejador).</summary>
    private async Task<string> GetJson(string url)
    {
        try
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Get, url);
            peticion.Headers.TryAddWithoutValidation("Accept", "application/json");
            peticion.Headers.TryAddWithoutValidation("User-Agent", "CountryAPI/1.0");
            using HttpResponseMessage respuesta = await http.SendAsync(peticion);
            int codigo = (int)respuesta.StatusCode;
            if (codigo != 200)
            {
                log.LogWarning("La API de universidades respondió {Codigo}", codigo);
                throw new IOException(NO_DISPONIBLE);
            }
            return await respuesta.Content.ReadAsStringAsync();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException
                                       or InvalidOperationException or UriFormatException
                                       || (e is IOException && e.Message != NO_DISPONIBLE))
        {
            log.LogWarning("No se pudo consultar la API de universidades: {Error}", e.Message);
            throw new IOException(NO_DISPONIBLE, e);
        }
    }
}
