using System.Reflection;

namespace ec.edu.monster.config;

/// <summary>
/// Dirección del servidor RESTful y tiempo de espera de las peticiones.
///
/// La dirección inicial sale de la propiedad <c>Servidor</c> del .csproj (por defecto
/// http://10.0.2.2:5088/api, que es el equipo anfitrión visto desde el emulador). El usuario la
/// puede cambiar en la pantalla de inicio de sesión sin recompilar; la nueva dirección se guarda
/// en las preferencias de la app (ConfigServidor.Preferencias.cs).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public static partial class ConfigServidor
{
    /// <summary>Dirección del emulador si la compilación no indica otra.</summary>
    private const string URL_EMULADOR = "http://10.0.2.2:5088/api";

    /// <summary>Dirección con la que se compiló la app.</summary>
    public static readonly string URL_POR_DEFECTO = QuitarBarraFinal(LeerUrlDeCompilacion());

    /// <summary>Tiempo máximo de espera de cada petición al servidor, en segundos.</summary>
    public const int TIMEOUT_SEGUNDOS = 15;

    /// <summary>True si el texto es una dirección http o https con equipo, por ejemplo http://192.168.1.20:5088/api</summary>
    public static bool EsUrlValida(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }
        // Como java.net.URI: un espacio dentro de la dirección la vuelve inválida.
        if (url.Trim().Any(char.IsWhiteSpace))
        {
            return false;
        }
        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrEmpty(uri.Host);
    }

    public static string QuitarBarraFinal(string url)
    {
        return url.TrimEnd('/');
    }

    /// <summary>La que dejó el .csproj (dotnet build -p:Servidor=...) en los metadatos del ensamblado.</summary>
    private static string LeerUrlDeCompilacion()
    {
        string? url = typeof(ConfigServidor).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "Servidor")?.Value;
        return string.IsNullOrWhiteSpace(url) ? URL_EMULADOR : url.Trim();
    }
}
