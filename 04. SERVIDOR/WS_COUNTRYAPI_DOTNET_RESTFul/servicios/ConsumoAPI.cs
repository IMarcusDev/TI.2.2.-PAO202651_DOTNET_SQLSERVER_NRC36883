using System.Globalization;
using System.Net;
using ec.edu.monster.modelos.data;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ec.edu.monster.servicios;

/// <summary>
/// Consumo de la API pública REST Countries (RQF-008, ECUD8).
///
/// Es el único lugar del sistema que conoce la dirección de la API y el formato de su JSON.
/// Hace peticiones GET con HttpClient, espera cada respuesta como máximo
/// <see cref="TIMEOUT_SEGUNDOS"/> segundos y convierte cada país del JSON en un objeto
/// <see cref="Pais"/> con Newtonsoft.Json. Nunca toca la base de datos.
///
/// <para><b>Versión v5.</b> La v3.1 que describe la documentación fue retirada por el proveedor
/// (DUDAS.md de TI.2.1, C1, y H2 de este proyecto). La v5 cambia lo siguiente:</para>
/// <list type="bullet">
///   <item>dirección https://api.restcountries.com/countries/v5;</item>
///   <item>clave obligatoria en el encabezado «Authorization: Bearer &lt;clave&gt;»;</item>
///   <item>respuesta {"data": {"objects": [...], "meta": {"more": ...}}}, paginada de 100 en 100;</item>
///   <item>nombres de campos nuevos (ver <see cref="Convertir"/>).</item>
/// </list>
///
/// <para>La dirección y la clave se leen en cada petición de la configuración:</para>
/// <list type="bullet">
///   <item>dirección: CountryApi:RestCountriesUrl (por defecto la de la v5);</item>
///   <item>clave: CountryApi:RestCountriesClave (dotnet user-secrets) o la variable de entorno
///         COUNTRYAPI_RESTCOUNTRIES_CLAVE. Nunca está escrita en el código ni en archivos versionados.</item>
/// </list>
///
/// <para>Errores que informa:</para>
/// <list type="bullet">
///   <item>sin clave, sin conexión, tiempo agotado o código distinto de 200 → IOException
///         «La API REST Countries no está disponible, intente más tarde» (502);</item>
///   <item>JSON que no tiene el formato esperado → IOException
///         «La API REST Countries devolvió una respuesta no válida» (502);</item>
///   <item>búsqueda por nombre sin coincidencias → KeyNotFoundException
///         «No se encontraron países con ese nombre en la API» (404).</item>
/// </list>
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ConsumoAPI
{
    internal const string NO_DISPONIBLE = "La API REST Countries no está disponible, intente más tarde";
    internal const string RESPUESTA_NO_VALIDA = "La API REST Countries devolvió una respuesta no válida";
    internal const string SIN_COINCIDENCIAS = "No se encontraron países con ese nombre en la API";

    /// <summary>Dirección de la v5 si la configuración no indica otra.</summary>
    public const string URL_POR_DEFECTO = "https://api.restcountries.com/countries/v5";

    /// <summary>Solo los campos que usa el sistema, para que la respuesta sea pequeña y rápida.</summary>
    public const string CAMPOS =
        "names.common,names.translations.spa,capitals,region,subregion,population,area,currencies";

    public const int TIMEOUT_SEGUNDOS = 15;
    /// <summary>Máximo de países por página en el plan gratuito.</summary>
    private const int POR_PAGINA = 100;
    /// <summary>Tope de páginas por importación (hoy son unos 250 países: 3 páginas).</summary>
    private const int MAXIMO_PAGINAS = 10;
    /// <summary>Largo máximo de las columnas NVARCHAR(100) de la tabla Paises.</summary>
    private const int LARGO_MAXIMO = 100;

    /// <summary>Regiones de la API (en inglés) y su nombre en el catálogo Regiones.</summary>
    private static readonly Dictionary<string, string> REGIONES = new()
    {
        ["Americas"] = "América",
        ["Europe"] = "Europa",
        ["Asia"] = "Asia",
        ["Africa"] = "África",
        ["Oceania"] = "Oceanía",
        ["Antarctic"] = "Antártida",
        ["Antarctica"] = "Antártida"
    };

    private readonly HttpClient http;
    private readonly IConfiguration? configuracion;
    private readonly string? urlFija;
    private readonly string? claveFija;
    private readonly ILogger log;

    /// <summary>Constructor que usa Program.cs: HttpClient de IHttpClientFactory y la configuración.</summary>
    public ConsumoAPI(HttpClient http, IConfiguration configuracion, ILogger<ConsumoAPI> log)
    {
        this.http = http;
        this.configuracion = configuracion;
        this.log = log;
    }

    /// <summary>Permite apuntar a otra dirección y usar otra clave (pruebas con un servidor falso).</summary>
    internal ConsumoAPI(string urlBase, string? clave)
    {
        http = new HttpClient { Timeout = TimeSpan.FromSeconds(TIMEOUT_SEGUNDOS) };
        urlFija = urlBase;
        claveFija = clave ?? "";
        log = NullLogger.Instance;
    }

    /// <summary>Para las APIs falsas de las pruebas, que reemplazan los métodos públicos.</summary>
    protected ConsumoAPI() : this("http://no-se-usa", null)
    {
    }

    /// <summary>Dirección base de la API, sin «/» al final; se lee en cada petición.</summary>
    public string URL_BASE
    {
        get
        {
            string url = urlFija ?? configuracion?["CountryApi:RestCountriesUrl"] ?? URL_POR_DEFECTO;
            if (string.IsNullOrWhiteSpace(url))
            {
                url = URL_POR_DEFECTO;
            }
            return url.Trim().TrimEnd('/');
        }
    }

    /// <summary>
    /// Todos los países (Importar Países). La API entrega como máximo 100 por página, así que
    /// se piden páginas con offset hasta que meta.more sea false.
    /// </summary>
    public virtual async Task<List<Pais>> ObtenerTodos()
    {
        var paises = new List<Pais>();
        int offset = 0;
        string urlBase = URL_BASE;
        for (int pagina = 0; pagina < MAXIMO_PAGINAS; pagina++)
        {
            var (codigo, cuerpo) = await Get(urlBase + "?limit=" + POR_PAGINA + "&offset=" + offset
                + "&response_fields=" + CAMPOS);
            if (codigo != 200)
            {
                log.LogWarning("REST Countries (listado) respondió {Codigo}", codigo);
                throw new IOException(NO_DISPONIBLE);
            }
            Pagina actual = LeerPagina(cuerpo);
            paises.AddRange(actual.Paises);
            if (!actual.HayMas || actual.Recibidos == 0)
            {
                return paises;
            }
            offset += actual.Recibidos;
        }
        log.LogWarning("REST Countries: se alcanzó el tope de páginas; la importación puede estar incompleta");
        return paises;
    }

    /// <summary>
    /// Países cuyo nombre (común, oficial, alternativo o nativo) contiene el texto
    /// (Consultar País en la API). Usa el agregado «name» de la v5.
    /// </summary>
    public virtual async Task<List<Pais>> BuscarPorNombre(string nombre)
    {
        // WebUtility.UrlEncode codifica el espacio como «+», igual que URLEncoder de Java.
        string q = WebUtility.UrlEncode(nombre.Trim());
        var (codigo, cuerpo) = await Get(URL_BASE + "/name?q=" + q + "&limit=" + POR_PAGINA
            + "&response_fields=" + CAMPOS);
        if (codigo == 404)
        {
            throw new KeyNotFoundException(SIN_COINCIDENCIAS);
        }
        if (codigo != 200)
        {
            log.LogWarning("REST Countries (búsqueda) respondió {Codigo}", codigo);
            throw new IOException(NO_DISPONIBLE);
        }
        List<Pais> paises = LeerPagina(cuerpo).Paises;
        if (paises.Count == 0)
        {
            throw new KeyNotFoundException(SIN_COINCIDENCIAS);
        }
        return paises;
    }

    /// <summary>Convierte el cuerpo de una respuesta de la v5 en países (se usa en las pruebas).</summary>
    internal List<Pais> ConvertirRespuesta(string json)
    {
        return LeerPagina(json).Paises;
    }

    /// <summary>
    /// Lee {"data": {"objects": [...], "meta": {"more": true|false}}}. Los países cuya región no
    /// está en el catálogo se descartan y se registran en el log (ECUD8, flujo alternativo 4).
    /// </summary>
    private Pagina LeerPagina(string json)
    {
        try
        {
            JToken? raiz = Parsear(json);
            JObject? data = (raiz as JObject)?["data"] as JObject;
            if (data?["objects"] is not JArray arreglo)
            {
                throw new IOException(RESPUESTA_NO_VALIDA);
            }
            var paises = new List<Pais>(arreglo.Count);
            foreach (JToken elemento in arreglo)
            {
                if (elemento is not JObject objeto)
                {
                    throw new IOException(RESPUESTA_NO_VALIDA);
                }
                Pais? pais = Convertir(objeto);
                if (pais != null)
                {
                    paises.Add(pais);
                }
            }
            JToken? mas = (data["meta"] as JObject)?["more"];
            bool hayMas = mas is JValue valor && valor.Type != JTokenType.Null && Booleano(valor);
            return new Pagina(paises, arreglo.Count, hayMas);
        }
        catch (Exception e) when (e is JsonException or InvalidCastException or FormatException
                                       or OverflowException or ArgumentException)
        {
            // El cuerpo no es JSON o algún campo no tiene el tipo esperado.
            throw new IOException(RESPUESTA_NO_VALIDA, e);
        }
    }

    /// <summary>
    /// Mapeo de un país del JSON de REST Countries v5 a los siete datos del taller
    /// (ERS, tabla 41, con los nombres de campo de la v5):
    /// <list type="bullet">
    ///   <item>nombre: names.translations.spa.common; si no existe, names.common;</item>
    ///   <item>capital: capitals[].name, la marcada como primary o la primera; «Sin capital» si no tiene;</item>
    ///   <item>región: region traducida al español (Americas → América, ...);</item>
    ///   <item>subregión: subregion tal como viene; «Sin subregión» si no tiene;</item>
    ///   <item>población: population como entero largo;</item>
    ///   <item>área: area.kilometers en km² (0 si no viene);</item>
    ///   <item>moneda: «name (code)» de la primera de currencies; «Sin moneda» si no tiene.</item>
    /// </list>
    /// </summary>
    /// <returns>el país, o null si su región no está en el catálogo</returns>
    public Pais? Convertir(JObject json)
    {
        string? nombre = Texto(json, "names", "translations", "spa", "common") ?? Texto(json, "names", "common");
        if (nombre == null)
        {
            throw new IOException(RESPUESTA_NO_VALIDA);
        }

        string? regionApi = Texto(json, "region");
        string? region = TraducirRegion(regionApi);
        if (region == null)
        {
            log.LogWarning("País descartado por región desconocida: {Nombre} ({Region})", nombre, regionApi);
            return null;
        }

        string? capital = CapitalPrincipal(json["capitals"]);
        string? subregion = Texto(json, "subregion");

        // Población y área no pueden ser negativas (CHECK de la tabla Paises).
        long poblacion = Math.Max(0L, NumeroLargo(json["population"]));
        double km2 = json["area"] is JObject area ? Math.Max(0.0, NumeroDoble(area["kilometers"])) : 0.0;

        return new Pais(
            Recortar(nombre),
            Recortar(capital ?? "Sin capital"),
            new Region(0, region),
            Recortar(subregion ?? "Sin subregión"),
            poblacion,
            km2,
            Recortar(Moneda(json["currencies"])));
    }

    /// <summary>Traduce la región de la API al español del catálogo; null si no la reconoce.</summary>
    public string? TraducirRegion(string? region)
    {
        return region != null && REGIONES.TryGetValue(region.Trim(), out string? traducida) ? traducida : null;
    }

    /// <summary>
    /// La v5 entrega capitals como [{"name": "Quito", "attributes": {"primary": true, ...}}].
    /// Se toma la capital marcada como primary; si ninguna lo está, la primera con nombre.
    /// </summary>
    private static string? CapitalPrincipal(JToken? capitales)
    {
        if (capitales is not JArray lista)
        {
            return null;
        }
        string? primera = null;
        foreach (JToken e in lista)
        {
            if (e is not JObject capital)
            {
                continue;
            }
            string? nombre = Texto(capital, "name");
            if (nombre == null)
            {
                continue;
            }
            JToken? principal = (capital["attributes"] as JObject)?["primary"];
            if (principal is JValue valor && valor.Type != JTokenType.Null && Booleano(valor))
            {
                return nombre;
            }
            primera ??= nombre;
        }
        return primera;
    }

    /// <summary>
    /// La v5 entrega currencies como [{"code": "USD", "name": "United States dollar", "symbol": "$"}].
    /// Se toma la primera moneda: «United States dollar (USD)».
    /// </summary>
    private static string Moneda(JToken? monedas)
    {
        if (monedas is not JArray lista)
        {
            return "Sin moneda";
        }
        foreach (JToken e in lista)
        {
            if (e is not JObject moneda)
            {
                continue;
            }
            string? codigo = Texto(moneda, "code");
            string? nombre = Texto(moneda, "name");
            if (nombre != null && codigo != null)
            {
                return nombre + " (" + codigo + ")";
            }
            if (nombre != null || codigo != null)
            {
                return nombre ?? codigo!;
            }
        }
        return "Sin moneda";
    }

    /// <summary>Texto en la ruta indicada (por ejemplo names → common), o null si no existe o está vacío.</summary>
    private static string? Texto(JObject json, params string[] ruta)
    {
        JToken? actual = json;
        foreach (string paso in ruta)
        {
            if (actual is not JObject objeto)
            {
                return null;
            }
            actual = objeto[paso];
        }
        if (actual is not JValue valor || valor.Type == JTokenType.Null)
        {
            return null;
        }
        string texto = ComoTexto(valor).Trim();
        return texto.Length == 0 ? null : texto;
    }

    /// <summary>Un valor simple como texto, como getAsString de Gson (true/false en minúsculas).</summary>
    private static string ComoTexto(JValue valor)
    {
        return valor.Type switch
        {
            JTokenType.Boolean => (bool)valor ? "true" : "false",
            _ => Convert.ToString(valor.Value, CultureInfo.InvariantCulture) ?? ""
        };
    }

    /// <summary>Como getAsBoolean de Gson: el booleano, o true si el texto es «true» (sin distinguir mayúsculas).</summary>
    private static bool Booleano(JValue valor)
    {
        return valor.Type == JTokenType.Boolean
            ? (bool)valor
            : string.Equals(ComoTexto(valor), "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Número entero del campo (se trunca si trae decimales), o 0 si no viene.</summary>
    private static long NumeroLargo(JToken? valor)
    {
        if (valor == null || valor.Type == JTokenType.Null)
        {
            return 0;
        }
        return valor.Type switch
        {
            JTokenType.Integer => Convert.ToInt64(((JValue)valor).Value, CultureInfo.InvariantCulture),
            JTokenType.Float => (long)Math.Truncate((double)valor),
            JTokenType.String => (long)Math.Truncate(decimal.Parse((string)valor!, NumberStyles.Float, CultureInfo.InvariantCulture)),
            _ => throw new FormatException("No es un número")
        };
    }

    /// <summary>Número decimal del campo, o 0 si no viene.</summary>
    private static double NumeroDoble(JToken? valor)
    {
        if (valor == null || valor.Type == JTokenType.Null)
        {
            return 0;
        }
        return valor.Type switch
        {
            JTokenType.Integer or JTokenType.Float => (double)valor,
            JTokenType.String => double.Parse((string)valor!, NumberStyles.Float, CultureInfo.InvariantCulture),
            _ => throw new FormatException("No es un número")
        };
    }

    private static string Recortar(string texto)
    {
        return texto.Length <= LARGO_MAXIMO ? texto : texto[..LARGO_MAXIMO];
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

    /// <summary>
    /// GET con la clave y tiempo de espera; cualquier falla de red se informa como API no
    /// disponible. Un 401 o 403 (clave inválida o cuota mensual agotada) también, con el detalle
    /// en el log.
    /// </summary>
    private async Task<(int Codigo, string Cuerpo)> Get(string url)
    {
        string? clave = Clave();
        if (string.IsNullOrWhiteSpace(clave))
        {
            log.LogError("Falta la clave de REST Countries (CountryApi:RestCountriesClave o COUNTRYAPI_RESTCOUNTRIES_CLAVE)");
            throw new IOException(NO_DISPONIBLE);
        }
        try
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Get, url);
            peticion.Headers.TryAddWithoutValidation("Accept", "application/json");
            peticion.Headers.TryAddWithoutValidation("User-Agent", "CountryAPI/1.0");
            peticion.Headers.TryAddWithoutValidation("Authorization", "Bearer " + clave);
            using HttpResponseMessage respuesta = await http.SendAsync(peticion);
            string cuerpo = await respuesta.Content.ReadAsStringAsync();
            int codigo = (int)respuesta.StatusCode;
            if (codigo is 401 or 403 or 429)
            {
                log.LogError("REST Countries rechazó la petición ({Codigo}): {Cuerpo}", codigo, cuerpo);
            }
            return (codigo, cuerpo);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException
                                       or InvalidOperationException or UriFormatException)
        {
            log.LogWarning("No se pudo consultar REST Countries: {Error}", e.Message);
            throw new IOException(NO_DISPONIBLE, e);
        }
    }

    /// <summary>Clave de la API: CountryApi:RestCountriesClave o la variable COUNTRYAPI_RESTCOUNTRIES_CLAVE.</summary>
    private string? Clave()
    {
        if (claveFija != null)
        {
            return claveFija.Trim();
        }
        string? clave = configuracion?["CountryApi:RestCountriesClave"];
        if (string.IsNullOrWhiteSpace(clave))
        {
            clave = Environment.GetEnvironmentVariable("COUNTRYAPI_RESTCOUNTRIES_CLAVE");
        }
        return clave?.Trim();
    }

    /// <summary>Países convertidos de una página, cuántos objetos traía y si hay más páginas.</summary>
    private sealed record Pagina(List<Pais> Paises, int Recibidos, bool HayMas);
}
