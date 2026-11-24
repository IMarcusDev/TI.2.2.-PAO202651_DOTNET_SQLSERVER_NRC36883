using ec.edu.monster.servicios;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.web;

/// <summary>
/// Estado del servidor RESTful para el indicador de la barra superior: responde
/// <c>{"disponible":true}</c> o <c>false</c>. La página lo consulta cada 10 segundos.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[Route("estado")]
public class EstadoController(ClienteRest cliente) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Consultar()
    {
        bool disponible = await cliente.ServidorDisponible();
        Response.Headers.CacheControl = "no-store";
        return Content("{\"disponible\":" + (disponible ? "true" : "false") + "}", "application/json; charset=utf-8");
    }
}
