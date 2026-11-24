using System.Data.Common;
using System.Text.Json.Serialization;
using ec.edu.monster.modelos.enums;
using Microsoft.EntityFrameworkCore;

namespace ec.edu.monster.modelos.dto;

/// <summary>
/// Respuesta estructurada de todas las operaciones del servidor (RQF-009, ECUD9):
/// {"estado": "OK" | "ERROR", "mensaje": "...", "datos": ...}.
/// En una respuesta con error, datos va en null; a diferencia de los objetos que van dentro de
/// datos, aquí los null sí se escriben ("datos": null), igual que en el servidor Java.
/// </summary>
/// <typeparam name="T">tipo de los datos de la operación</typeparam>
/// <remarks>Autor: Escobar Marcos</remarks>
public class RespuestaDTO<T>
{
    public EstadoRespuesta Estado { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Mensaje { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public T? Datos { get; set; }

    public RespuestaDTO()
    {
    }

    public RespuestaDTO(EstadoRespuesta estado, string? mensaje, T? datos)
    {
        Estado = estado;
        Mensaje = mensaje;
        Datos = datos;
    }
}

/// <summary>
/// Fábrica de respuestas y decisión del código HTTP de cada error, según la excepción que lanzó
/// el controlador (decisión A2 de TI.2.1, con las excepciones equivalentes de .NET):
/// <list type="bullet">
///   <item>ArgumentException → 400 (datos inválidos)</item>
///   <item>UnauthorizedAccessException → 401 (credenciales o token)</item>
///   <item>KeyNotFoundException → 404 (recurso inexistente)</item>
///   <item>InvalidOperationException → 409 (país repetido)</item>
///   <item>HttpRequestException / TaskCanceledException / IOException → 502 (falla de una API externa)</item>
///   <item>error de la base de datos → 500 «Servicio no disponible, intente más tarde»</item>
///   <item>cualquier otra → 500 «Ocurrió un error inesperado, intente nuevamente»</item>
/// </list>
/// Así el mensaje nunca expone SQL ni trazas: para los 500 se usa un texto fijo.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public static class RespuestaDTO
{
    public const string TOKEN_INVALIDO = "Token inválido o sesión expirada";
    public const string SERVICIO_NO_DISPONIBLE = "Servicio no disponible, intente más tarde";
    public const string ERROR_INESPERADO = "Ocurrió un error inesperado, intente nuevamente";

    /// <summary>Respuesta correcta con su mensaje y sus datos.</summary>
    public static RespuestaDTO<T> Ok<T>(string mensaje, T? datos) => new(EstadoRespuesta.OK, mensaje, datos);

    /// <summary>Respuesta de error: los datos quedan vacíos.</summary>
    public static RespuestaDTO<object> Error(string mensaje) => new(EstadoRespuesta.ERROR, mensaje, null);

    /// <summary>Convierte la excepción de un controlador en la respuesta de error que verá el cliente.</summary>
    public static RespuestaDTO<object> DeExcepcion(Exception e) => Error(MensajeDe(e));

    /// <summary>Código HTTP que acompaña a la respuesta de error de esa excepción.</summary>
    public static int CodigoHttp(Exception e)
    {
        // Primero los errores de la base: EF Core a veces los envuelve en InvalidOperationException,
        // que de otro modo se confundiría con el 409.
        if (EsErrorDeBaseDeDatos(e))
        {
            return 500;
        }
        return e switch
        {
            ArgumentException => 400,
            UnauthorizedAccessException => 401,
            KeyNotFoundException => 404,
            InvalidOperationException => 409,
            HttpRequestException or TaskCanceledException or IOException => 502,
            _ => 500
        };
    }

    /// <summary>True si el error no es de negocio y conviene dejar el detalle en el log del servidor.</summary>
    public static bool EsErrorInterno(Exception e) => CodigoHttp(e) == 500;

    private static string MensajeDe(Exception e)
    {
        if (EsErrorDeBaseDeDatos(e))
        {
            return SERVICIO_NO_DISPONIBLE;
        }
        if (CodigoHttp(e) == 500 || string.IsNullOrWhiteSpace(e.Message))
        {
            return ERROR_INESPERADO;
        }
        return e.Message;
    }

    /// <summary>
    /// Equivalente de SQLException en Java: una excepción del proveedor de la base (DbException,
    /// por ejemplo SqlException) o de EF Core al guardar (DbUpdateException), sola o envuelta.
    /// </summary>
    private static bool EsErrorDeBaseDeDatos(Exception e)
    {
        for (Exception? actual = e; actual != null; actual = actual.InnerException)
        {
            if (actual is DbException or DbUpdateException)
            {
                return true;
            }
        }
        return false;
    }
}
