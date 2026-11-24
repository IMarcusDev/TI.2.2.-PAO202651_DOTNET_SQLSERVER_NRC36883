using ec.edu.monster.config;
using ec.edu.monster.servicios;

namespace ec.edu.monster;

/// <summary>
/// Crea una sola vez el modelo del cliente: el <see cref="ClienteRest"/>, la sesión y los
/// servicios de cada módulo. La página principal toma estos objetos para armar los controladores.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class AplicacionCountryApi
{
    public ClienteRest Cliente { get; }
    public SesionServicio Sesion { get; } = new();
    public LoginServicio LoginServicio { get; }
    public PaisServicio PaisServicio { get; }
    public ReporteServicio ReporteServicio { get; }
    public UniversidadServicio UniversidadServicio { get; }

    public AplicacionCountryApi()
    {
        Cliente = new ClienteRest(ConfigServidor.LeerUrlBase());
        LoginServicio = new LoginServicio(Cliente);
        PaisServicio = new PaisServicio(Cliente);
        ReporteServicio = new ReporteServicio(Cliente);
        UniversidadServicio = new UniversidadServicio(Cliente);
    }

    /// <summary>Guarda la nueva dirección del servidor y apunta el cliente a ella.</summary>
    public string CambiarServidor(string url)
    {
        string normalizada = ConfigServidor.GuardarUrlBase(url);
        Cliente.UrlBase = normalizada;
        return normalizada;
    }
}
