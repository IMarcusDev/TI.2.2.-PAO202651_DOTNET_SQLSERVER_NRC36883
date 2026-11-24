using ec.edu.monster.filtros;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.web;

/// <summary>
/// Página para 403, 404 y 500 (el error.jsp de Java): nunca muestra trazas. La usan
/// UseStatusCodePagesWithReExecute y UseExceptionHandler, que la vuelven a ejecutar con el
/// método original; por eso acepta cualquier método y no pide token CSRF.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[SinCsrf]
public class ErrorController : Controller
{
    [Route("error/{codigo:int}")]
    public IActionResult Mostrar(int codigo)
    {
        ViewData["codigo"] = codigo;
        return View(Pagina.CARPETA + "error.cshtml");
    }
}
