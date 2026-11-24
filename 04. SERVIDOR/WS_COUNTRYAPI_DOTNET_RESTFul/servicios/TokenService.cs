using System.Collections.Concurrent;
using ec.edu.monster.modelos.data;

namespace ec.edu.monster.servicios;

/// <summary>
/// Sesiones de los usuarios, guardadas en memoria (RQF-007, RNF-005).
/// <list type="bullet">
///   <item>El token es un UUID que se entrega al iniciar sesión.</item>
///   <item>Expira tras <see cref="MINUTOS_EXPIRACION"/> minutos sin uso; cada uso válido renueva ese tiempo.</item>
///   <item>Llega en el encabezado Authorization, con o sin el prefijo «Bearer ».</item>
///   <item>Al reiniciar el servidor se pierden todas las sesiones.</item>
/// </list>
/// Program.cs lo registra como singleton: todos los servicios web comparten la misma instancia.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class TokenService
{
    public const int MINUTOS_EXPIRACION = 15;

    private readonly ConcurrentDictionary<string, Sesion> sesiones = new();
    private readonly TimeProvider reloj;

    public TokenService() : this(TimeProvider.System)
    {
    }

    /// <summary>El reloj se puede reemplazar en las pruebas para simular el paso del tiempo.</summary>
    internal TokenService(TimeProvider reloj)
    {
        this.reloj = reloj;
    }

    /// <summary>Registra una sesión nueva para el usuario y devuelve su token.</summary>
    public string GenerarToken(Usuario usuario)
    {
        EliminarExpirados();
        string token = Guid.NewGuid().ToString();
        sesiones[token] = new Sesion(token, usuario, Ahora());
        return token;
    }

    /// <summary>
    /// Comprueba el token y, si es válido, renueva la hora del último uso.
    /// </summary>
    /// <param name="token">valor del encabezado Authorization, con o sin «Bearer »</param>
    /// <returns>el usuario de la sesión, o null si el token falta, no existe o expiró</returns>
    public Usuario? ValidarToken(string? token)
    {
        string? limpio = Limpiar(token);
        if (limpio == null || !sesiones.TryGetValue(limpio, out Sesion? sesion))
        {
            return null;
        }
        DateTime ahora = Ahora();
        if (Expirada(sesion, ahora))
        {
            sesiones.TryRemove(limpio, out _);
            return null;
        }
        sesion.UltimoUso = ahora;
        return sesion.Usuario;
    }

    /// <summary>Cierra la sesión del token. No falla si el token ya no existe.</summary>
    public void EliminarToken(string? token)
    {
        string? limpio = Limpiar(token);
        if (limpio != null)
        {
            sesiones.TryRemove(limpio, out _);
        }
    }

    /// <summary>Quita el prefijo «Bearer » (sin distinguir mayúsculas) y los espacios; null si queda vacío.</summary>
    private static string? Limpiar(string? token)
    {
        if (token == null)
        {
            return null;
        }
        string valor = token.Trim();
        if (valor.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
        {
            valor = valor["bearer ".Length..].Trim();
        }
        return valor.Length == 0 ? null : valor;
    }

    private static bool Expirada(Sesion sesion, DateTime ahora)
    {
        return sesion.UltimoUso.AddMinutes(MINUTOS_EXPIRACION) < ahora;
    }

    /// <summary>Libera la memoria de las sesiones abandonadas.</summary>
    private void EliminarExpirados()
    {
        DateTime ahora = Ahora();
        foreach (var par in sesiones)
        {
            if (Expirada(par.Value, ahora))
            {
                sesiones.TryRemove(par.Key, out _);
            }
        }
    }

    private DateTime Ahora() => reloj.GetLocalNow().DateTime;
}
