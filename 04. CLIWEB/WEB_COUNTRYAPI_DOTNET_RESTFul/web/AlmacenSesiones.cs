using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ec.edu.monster.web;

/// <summary>
/// Sesión de un usuario en el servidor (la HttpSession de Java): atributos con objetos vivos
/// (sus controladores, el token, los avisos «flash»). El navegador solo guarda su id en una
/// cookie HttpOnly.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public sealed class SesionWeb
{
    private readonly ConcurrentDictionary<string, object> atributos = new();

    internal SesionWeb(string id, DateTimeOffset ahora)
    {
        Id = id;
        UltimoAcceso = ahora;
    }

    public string Id { get; internal set; }

    internal DateTimeOffset UltimoAcceso { get; set; }

    public object? Leer(string nombre) => atributos.GetValueOrDefault(nombre);

    public void Poner(string nombre, object valor) => atributos[nombre] = valor;

    public void Quitar(string nombre) => atributos.TryRemove(nombre, out _);
}

/// <summary>
/// Guarda las sesiones de todos los usuarios en memoria, como el contenedor de servlets:
/// <list type="bullet">
///   <item>la sesión caduca a los 15 minutos sin uso, igual que el token del servidor;</item>
///   <item>el id va solo en la cookie HttpOnly «CountryAPI.Sesion», nunca en la URL;</item>
///   <item>al iniciar sesión se cambia el id (evita la fijación de sesión) y al cerrarla se
///         invalida.</item>
/// </list>
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public sealed class AlmacenSesiones(TimeProvider reloj)
{
    public const string COOKIE = "CountryAPI.Sesion";
    public static readonly TimeSpan DURACION = TimeSpan.FromMinutes(15);

    private const string EN_PETICION = "sesionWeb";
    private readonly ConcurrentDictionary<string, SesionWeb> sesiones = new();
    private DateTimeOffset ultimaLimpieza;

    public AlmacenSesiones() : this(TimeProvider.System)
    {
    }

    /// <summary>La sesión de esta petición; si no hay y <paramref name="crear"/> es true, se crea (getSession(crear)).</summary>
    public SesionWeb? Obtener(HttpContext contexto, bool crear)
    {
        if (contexto.Items.TryGetValue(EN_PETICION, out object? actual) && actual is SesionWeb enPeticion)
        {
            return enPeticion;
        }
        DateTimeOffset ahora = reloj.GetUtcNow();
        Limpiar(ahora);
        SesionWeb? sesion = null;
        if (contexto.Request.Cookies.TryGetValue(COOKIE, out string? id) && id != null
            && sesiones.TryGetValue(id, out SesionWeb? guardada))
        {
            if (ahora - guardada.UltimoAcceso > DURACION)
            {
                sesiones.TryRemove(id, out _);
            }
            else
            {
                sesion = guardada;
            }
        }
        if (sesion == null && crear)
        {
            sesion = new SesionWeb(NuevoId(), ahora);
            sesiones[sesion.Id] = sesion;
            EscribirCookie(contexto, sesion.Id);
        }
        if (sesion != null)
        {
            sesion.UltimoAcceso = ahora;
            contexto.Items[EN_PETICION] = sesion;
        }
        return sesion;
    }

    /// <summary>Nuevo id para la misma sesión (changeSessionId), con su cookie.</summary>
    public void CambiarId(HttpContext contexto, SesionWeb sesion)
    {
        sesiones.TryRemove(sesion.Id, out _);
        sesion.Id = NuevoId();
        sesiones[sesion.Id] = sesion;
        EscribirCookie(contexto, sesion.Id);
    }

    /// <summary>Cierra la sesión de esta petición y borra la cookie (invalidate).</summary>
    public void Invalidar(HttpContext contexto)
    {
        SesionWeb? sesion = Obtener(contexto, false);
        if (sesion != null)
        {
            sesiones.TryRemove(sesion.Id, out _);
        }
        contexto.Items.Remove(EN_PETICION);
        contexto.Response.Cookies.Delete(COOKIE, new CookieOptions { Path = "/" });
    }

    private static void EscribirCookie(HttpContext contexto, string id)
    {
        contexto.Response.Cookies.Append(COOKIE, id, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true
        });
    }

    private static string NuevoId()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Quita de vez en cuando las sesiones caducadas.</summary>
    private void Limpiar(DateTimeOffset ahora)
    {
        if (ahora - ultimaLimpieza < TimeSpan.FromMinutes(1))
        {
            return;
        }
        ultimaLimpieza = ahora;
        foreach (KeyValuePair<string, SesionWeb> par in sesiones)
        {
            if (ahora - par.Value.UltimoAcceso > DURACION)
            {
                sesiones.TryRemove(par.Key, out _);
            }
        }
    }
}
