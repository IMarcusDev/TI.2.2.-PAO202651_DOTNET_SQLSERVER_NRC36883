namespace ec.edu.monster.config;

/// <summary>
/// La parte de ConfigServidor que usa las preferencias del dispositivo (equivale a las
/// SharedPreferences de Android). Va aparte porque las pruebas corren en el equipo, sin MAUI.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public static partial class ConfigServidor
{
    private const string PREFERENCIAS = "countryapi";
    private const string CLAVE_URL = "servidor.url";

    /// <summary>URL base del servidor, sin «/» al final: la guardada por el usuario o la de compilación.</summary>
    public static string LeerUrlBase()
    {
        string? guardada = Preferences.Default.Get<string?>(CLAVE_URL, null, PREFERENCIAS);
        return string.IsNullOrWhiteSpace(guardada) ? URL_POR_DEFECTO : guardada;
    }

    /// <summary>Guarda la nueva dirección (ya validada con <see cref="EsUrlValida"/>) y la devuelve normalizada.</summary>
    public static string GuardarUrlBase(string url)
    {
        string normalizada = QuitarBarraFinal(url.Trim());
        Preferences.Default.Set(CLAVE_URL, normalizada, PREFERENCIAS);
        return normalizada;
    }
}
