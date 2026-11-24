using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ec.edu.monster.filtros;

/// <summary>
/// Protección CSRF de todos los POST (el CsrfFilter de Java), con el Antiforgery de ASP.NET Core:
/// cada formulario lleva el campo <c>_csrf</c> (<c>@Html.AntiForgeryToken()</c>) y aquí se
/// comprueba contra su cookie antes de llegar al controlador.
/// <list type="bullet">
///   <item>POST /login con un token vencido o falso: se muestra un formulario nuevo;</item>
///   <item>cualquier otro POST sin token válido: 403 (página de error propia).</item>
/// </list>
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public sealed class CsrfFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    public const string PARAMETRO = "_csrf";
    private const string RUTA_LOGIN = "/login";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext contexto)
    {
        HttpRequest peticion = contexto.HttpContext.Request;
        if (!HttpMethods.IsPost(peticion.Method)
            || contexto.ActionDescriptor.EndpointMetadata.OfType<SinCsrfAttribute>().Any())
        {
            return;
        }
        if (await antiforgery.IsRequestValidAsync(contexto.HttpContext))
        {
            return;
        }
        contexto.Result = string.Equals(peticion.Path, RUTA_LOGIN, StringComparison.OrdinalIgnoreCase)
            ? new RedirectResult(RUTA_LOGIN)
            : new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}

/// <summary>Excluye una acción de <see cref="CsrfFilter"/> (la página de error, que se vuelve a ejecutar con el método original).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SinCsrfAttribute : Attribute
{
}
