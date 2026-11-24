using System.Diagnostics;
using System.Net;
using System.Text;
using ec.edu.monster.config;
using ec.edu.monster.modelos;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ec.edu.monster.servicios;

/// <summary>
/// Único punto del cliente que habla con el servidor RESTful (HttpClient + Newtonsoft.Json).
///
/// Siempre devuelve una <see cref="RespuestaDTO{T}"/>, nunca lanza excepciones a los controladores:
/// <list type="bullet">
///   <item>si el servidor responde, su JSON {estado, mensaje, datos};</item>
///   <item>si una petición con token recibe 401, un ERROR «Su sesión expiró o no es válida...»
///         marcado como sesión expirada;</item>
///   <item>si no hay conexión o se agota el tiempo, un ERROR «No se pudo conectar con el
///         servidor...».</item>
/// </list>
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ClienteRest
{
    public const string SIN_CONEXION =
        "No se pudo conectar con el servidor. Verifique la red e intente nuevamente.";
    public const string SESION_EXPIRADA = "Su sesión expiró o no es válida. Inicie sesión nuevamente.";
    public const string RESPUESTA_NO_VALIDA = "Ocurrió un error inesperado, intente nuevamente";

    private const int ESPERA_ESTADO_SEGUNDOS = 3;

    /// <summary>JSON del contrato: claves en camelCase y sin los campos nulos (como Gson en Java).</summary>
    private static readonly JsonSerializerSettings JSON = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };

    /// <summary>Puede cambiar en tiempo de ejecución (pantalla de inicio de sesión → «Cambiar servidor»).</summary>
    private volatile string urlBase = "";
    private readonly HttpClient http;

    /// <summary>Cliente apuntado a la dirección indicada, con el tiempo de espera de ConfigServidor.</summary>
    public ClienteRest(string urlBase) : this(urlBase, ConfigServidor.TIMEOUT_SEGUNDOS)
    {
    }

    public ClienteRest(string urlBase, int timeoutSegundos)
    {
        UrlBase = urlBase;
        http = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSegundos) };
    }

    /// <summary>Dirección del servidor; al cambiarla, las peticiones siguientes ya usan la nueva.</summary>
    public string UrlBase
    {
        get => urlBase;
        set => urlBase = ConfigServidor.QuitarBarraFinal(value.Trim());
    }

    /// <summary>
    /// Envía una petición y convierte la respuesta.
    /// </summary>
    /// <param name="metodo">GET, POST, PUT o DELETE</param>
    /// <param name="ruta">ruta desde /api, por ejemplo "/Paises/buscar?region=Europa"</param>
    /// <param name="token">token de la sesión, o null (inicio de sesión)</param>
    /// <param name="cuerpo">objeto que se envía como JSON, o null</param>
    public async Task<RespuestaDTO<T>> Enviar<T>(string metodo, string ruta, string? token, object? cuerpo)
    {
        try
        {
            using var peticion = new HttpRequestMessage(new HttpMethod(metodo), urlBase + ruta);
            peticion.Headers.TryAddWithoutValidation("Accept", "application/json");
            if (token != null)
            {
                peticion.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
            }
            if (cuerpo != null || metodo == "POST" || metodo == "PUT")
            {
                // POST y PUT siempre llevan cuerpo (aunque vacío) para enviar Content-Length.
                string json = cuerpo == null ? "" : JsonConvert.SerializeObject(cuerpo, JSON);
                peticion.Content = new StringContent(json, new UTF8Encoding(false), "application/json");
            }
            using HttpResponseMessage respuesta = await http.SendAsync(peticion).ConfigureAwait(false);
            int codigo = (int)respuesta.StatusCode;
            if (token != null && codigo == 401)
            {
                RespuestaDTO<T> expirada = RespuestaDTO<T>.Error(SESION_EXPIRADA);
                expirada.CodigoHttp = codigo;
                expirada.SesionExpirada = true;
                return expirada;
            }
            return await LeerRespuesta<T>(respuesta).ConfigureAwait(false);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException
                                       or UriFormatException or InvalidOperationException)
        {
            Debug.WriteLine("Sin conexión con " + urlBase + ruta + ": " + e.Message);
            return RespuestaDTO<T>.Error(SIN_CONEXION);
        }
    }

    /// <summary>Convierte el cuerpo de la respuesta en una RespuestaDTO con su código HTTP.</summary>
    public async Task<RespuestaDTO<T>> LeerRespuesta<T>(HttpResponseMessage respuesta)
    {
        int codigo = (int)respuesta.StatusCode;
        string json = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
        RespuestaDTO<T>? convertida;
        try
        {
            convertida = JsonConvert.DeserializeObject<RespuestaDTO<T>>(json, JSON);
        }
        catch (JsonException e)
        {
            Debug.WriteLine("JSON no válido del servidor (HTTP " + codigo + "): " + e.Message);
            convertida = null;
        }
        if (convertida?.Estado == null)
        {
            convertida = RespuestaDTO<T>.Error(RESPUESTA_NO_VALIDA);
        }
        convertida.CodigoHttp = codigo;
        return convertida;
    }

    /// <summary>True si el servidor responde ahora mismo (cualquier código menor que 500).</summary>
    public async Task<bool> ServidorDisponible()
    {
        try
        {
            using var rapido = new HttpClient { Timeout = TimeSpan.FromSeconds(ESPERA_ESTADO_SEGUNDOS) };
            using HttpResponseMessage respuesta = await rapido.GetAsync(urlBase + "/Paises").ConfigureAwait(false);
            return (int)respuesta.StatusCode < 500;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException
                                       or UriFormatException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>Codifica un valor para la URL (query o ruta): los espacios quedan como %20.</summary>
    public static string Codificar(string valor)
    {
        return WebUtility.UrlEncode(valor).Replace("+", "%20");
    }
}
