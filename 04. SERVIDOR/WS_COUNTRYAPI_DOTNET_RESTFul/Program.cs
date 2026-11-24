using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using ec.edu.monster.config;
using ec.edu.monster.controladores;
using ec.edu.monster.modelos.interfaces;
using ec.edu.monster.modelos.repositorios;
using ec.edu.monster.servicios;
using Microsoft.EntityFrameworkCore;

namespace ec.edu.monster;

/// <summary>
/// Punto de entrada del servidor RESTful de CountryAPI (equivale a ApplicationConfig de TI.2.1).
/// Publica los servicios en http://&lt;servidor&gt;:5088/api con Kestrel y conecta las capas con
/// inyección de dependencias.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Clave de REST Countries guardada con «dotnet user-secrets» (también fuera de Development).
        builder.Configuration.AddUserSecrets<Program>(optional: true);
        // Archivo opcional, fuera de git, para apuntar las APIs externas a otra dirección sin
        // reiniciar (la suite curl lo usa para simular una API caída, DUDAS.md H10).
        builder.Configuration.AddJsonFile("appsettings.Pruebas.json", optional: true, reloadOnChange: true);

        RegistrarServicios(builder);

        WebApplication app = builder.Build();
        app.MapControllers();
        app.Run();
    }

    /// <summary>Registra el contexto de EF Core, los DAO, los controladores y los servicios.</summary>
    public static void RegistrarServicios(WebApplicationBuilder builder)
    {
        IServiceCollection servicios = builder.Services;

        // JSON del contrato idéntico al del servidor Java (DUDAS.md, H8).
        servicios.AddControllers().AddJsonOptions(opciones =>
        {
            opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            opciones.JsonSerializerOptions.Converters.Add(new DoubleFormatoJava());
            opciones.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            opciones.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        });

        string cadena = builder.Configuration.GetConnectionString("ApiPaisesDB")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:ApiPaisesDB en appsettings.json");
        servicios.AddDbContext<AppDbContext>(o => o.UseSqlServer(cadena));

        // DAO
        servicios.AddScoped<IUsuarioDAO, UsuarioDAO>();
        servicios.AddScoped<IRegionDAO, RegionDAO>();
        servicios.AddScoped<IPaisDAO, PaisDAO>();
        servicios.AddScoped<IUniversidadDAO, UniversidadDAO>();

        // Servicios: TokenService es único para que todos los servicios web compartan las sesiones.
        servicios.AddSingleton<TokenService>();
        servicios.AddSingleton<HashService>();
        servicios.AddHttpClient<ConsumoAPI>(http => http.Timeout = TimeSpan.FromSeconds(ConsumoAPI.TIMEOUT_SEGUNDOS));
        servicios.AddHttpClient<ConsumoAPIUniversidades>(http =>
                http.Timeout = TimeSpan.FromSeconds(ConsumoAPIUniversidades.TIMEOUT_SEGUNDOS))
            .ConfigurePrimaryHttpMessageHandler(ConsumoAPIUniversidades.CrearManejador);

        // Controladores de negocio
        servicios.AddScoped<LoginControlador>();
        servicios.AddScoped<PaisControlador>();
        servicios.AddScoped<ReporteControlador>();
        servicios.AddScoped<UniversidadControlador>();
    }
}
