using Newtonsoft.Json;

namespace ec.edu.monster.modelos;

/// <summary>
/// Respuesta estructurada del servidor: {"estado", "mensaje", "datos"}.
///
/// Además guarda el código HTTP (no viene en el JSON) para que los controladores sepan si la
/// sesión expiró (401 con token) y deban volver al inicio de sesión.
/// </summary>
/// <typeparam name="T">tipo de los datos</typeparam>
/// <remarks>Autor: Escobar Marcos</remarks>
public class RespuestaDTO<T>
{
    /// <summary>OK o ERROR; null si el JSON no traía estado (respuesta no válida).</summary>
    public EstadoRespuesta? Estado { get; set; }
    public string? Mensaje { get; set; }
    public T? Datos { get; set; }

    /// <summary>Código HTTP de la respuesta; 0 si no hubo conexión. No se lee del JSON.</summary>
    [JsonIgnore]
    public int CodigoHttp { get; set; }

    /// <summary>True cuando el servidor rechazó el token de la sesión.</summary>
    [JsonIgnore]
    public bool SesionExpirada { get; set; }

    public RespuestaDTO()
    {
    }

    public RespuestaDTO(EstadoRespuesta estado, string? mensaje, T? datos)
    {
        Estado = estado;
        Mensaje = mensaje;
        Datos = datos;
    }

    /// <summary>Respuesta de error creada por el propio cliente (por ejemplo, sin conexión).</summary>
    public static RespuestaDTO<T> Error(string mensaje) => new(EstadoRespuesta.ERROR, mensaje, default);

    public bool EsOk() => Estado == EstadoRespuesta.OK;
}
