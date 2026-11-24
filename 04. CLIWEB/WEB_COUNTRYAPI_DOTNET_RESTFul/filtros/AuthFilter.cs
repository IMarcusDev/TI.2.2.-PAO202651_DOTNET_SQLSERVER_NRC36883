using ec.edu.monster.web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ec.edu.monster.filtros;

/// <summary>
/// Control de acceso de todas las pantallas protegidas, antes de llegar al controlador: sin una
/// sesión con token del servidor se redirige al inicio de sesión.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AuthFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext contexto)
    {
        if (!ModulosSesion.ConSesion(contexto.HttpContext))
        {
            contexto.Result = new RedirectResult("/login");
        }
    }
}
