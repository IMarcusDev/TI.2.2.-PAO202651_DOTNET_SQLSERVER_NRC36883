using System.Text.Encodings.Web;
using System.Text.Unicode;
using ec.edu.monster.filtros;
using Microsoft.Extensions.WebEncoders;
using ec.edu.monster.servicios;
using ec.edu.monster.web;

namespace ec.edu.monster;

/// <summary>
/// Arranque del cliente web (el web.xml y el AplicacionWeb de Java): un solo
/// <see cref="ClienteRest"/> compartido por todos los usuarios, la sesión de cada usuario en el
/// servidor (15 minutos, como el token), CSRF en todos los POST y las páginas de error propias.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class Program
{
    public static void Main(string[] args)
    {
        WebApplication app = Construir(WebApplication.CreateBuilder(args));
        app.Logger.LogInformation("CountryAPI web: servidor RESTful en {Url}",
            app.Services.GetRequiredService<ClienteRest>().UrlBase);
        app.Run();
    }

    /// <summary>Registra los servicios y arma la canalización; las pruebas la usan con otro ClienteRest.</summary>
    public static WebApplication Construir(WebApplicationBuilder builder)
    {
        // Cliente sin estado, seguro para varios hilos, apuntado a appsettings.json (Servidor:Url).
        builder.Services.AddSingleton(_ => new ClienteRest());
        builder.Services.AddSingleton<AlmacenSesiones>();
        builder.Services.AddAntiforgery(opciones =>
        {
            opciones.FormFieldName = CsrfFilter.PARAMETRO;
            opciones.Cookie.Name = "CountryAPI.Csrf";
            opciones.Cookie.HttpOnly = true;
            opciones.Cookie.SameSite = SameSiteMode.Strict;
        });
        builder.Services.AddControllersWithViews(opciones => opciones.Filters.Add<CsrfFilter>());
        // Las tildes y la ñ salen tal cual en el HTML (como c:out); <, >, & y comillas se siguen escapando.
        builder.Services.Configure<WebEncoderOptions>(opciones =>
            opciones.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

        WebApplication app = builder.Build();
        // Errores con el diseño de la aplicación (403, 404 y 500), nunca con trazas.
        app.UseExceptionHandler("/error/500");
        app.UseStatusCodePagesWithReExecute("/error/{0}");
        app.UseStaticFiles();
        app.UseRouting();
        app.MapControllers();
        return app;
    }
}
