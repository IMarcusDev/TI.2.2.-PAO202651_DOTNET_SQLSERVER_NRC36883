using System.Net;
using System.Text.RegularExpressions;
using ec.edu.monster.filtros;
using ec.edu.monster.servicios;
using ec.edu.monster.web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ec.edu.monster.dobles;

/// <summary>
/// Navegador en memoria para probar el cliente web completo (filtros, controladores MVC y vistas
/// Razor) sin Kestrel: guarda las cookies, no sigue las redirecciones y recuerda el último token
/// CSRF de la página para enviarlo en los POST. El cliente web apunta al servidor RESTful falso.
/// </summary>
public sealed partial class Navegador : IDisposable
{
    /// <summary>Respuesta de una página: código, redirección (Location) y HTML.</summary>
    public sealed record Resultado(int Codigo, string? Redireccion, string Html, IReadOnlyList<string> CookiesSesion);

    private readonly Fabrica fabrica;
    private readonly HttpClient cliente;
    private string? token;

    public Navegador(string urlServidor)
    {
        fabrica = new Fabrica(urlServidor);
        cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    }

    public Task<Resultado> Get(string ruta) => Enviar(new HttpRequestMessage(HttpMethod.Get, ruta));

    /// <summary>POST de un formulario con pares nombre, valor; lleva el token CSRF de la última página.</summary>
    public async Task<Resultado> Post(string ruta, params string[] pares)
    {
        if (token == null)
        {
            await Get("/login");
        }
        var campos = new List<KeyValuePair<string, string>> { new(CsrfFilter.PARAMETRO, token!) };
        return await PostSinToken(ruta, campos, pares);
    }

    /// <summary>POST sin token CSRF (o con el que se indique en los pares).</summary>
    public Task<Resultado> PostSinToken(string ruta, params string[] pares) => PostSinToken(ruta, [], pares);

    public void Dispose()
    {
        cliente.Dispose();
        fabrica.Dispose();
    }

    private Task<Resultado> PostSinToken(string ruta, List<KeyValuePair<string, string>> campos, string[] pares)
    {
        for (int i = 0; i + 1 < pares.Length; i += 2)
        {
            campos.Add(new(pares[i], pares[i + 1]));
        }
        return Enviar(new HttpRequestMessage(HttpMethod.Post, ruta) { Content = new FormUrlEncodedContent(campos) });
    }

    private async Task<Resultado> Enviar(HttpRequestMessage peticion)
    {
        using HttpResponseMessage respuesta = await cliente.SendAsync(peticion);
        string html = await respuesta.Content.ReadAsStringAsync();
        Match encontrado = TokenCsrf().Match(html);
        if (encontrado.Success)
        {
            token = WebUtility.HtmlDecode(encontrado.Groups[1].Value);
        }
        List<string> cookies = respuesta.Headers.TryGetValues("Set-Cookie", out var valores)
            ? valores.Where(v => v.StartsWith(AlmacenSesiones.COOKIE + "=", StringComparison.Ordinal))
                .Select(v => v[(AlmacenSesiones.COOKIE.Length + 1)..].Split(';')[0]).ToList()
            : [];
        return new Resultado((int)respuesta.StatusCode, respuesta.Headers.Location?.OriginalString, html, cookies);
    }

    [GeneratedRegex("name=\"_csrf\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenCsrf();

    /// <summary>El cliente web con su ClienteRest apuntado al servidor falso.</summary>
    private sealed class Fabrica(string urlServidor) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<ClienteRest>();
                servicios.AddSingleton(new ClienteRest(urlServidor, 5));
            });
        }
    }
}
