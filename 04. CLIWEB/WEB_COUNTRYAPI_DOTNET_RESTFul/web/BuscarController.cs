using ec.edu.monster.filtros;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.web;

/// <summary>
/// «Buscar y filtrar» (ECUD5). Cada GET busca con los criterios de la dirección (?nombre,
/// ?capital, ?region); sin criterios muestra todos, como al entrar en escritorio.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[AuthFilter]
[Route("buscar")]
public class BuscarController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Mostrar()
    {
        ModulosSesion modulos = ModulosSesion.De(HttpContext);
        await modulos.Ejecutar(HttpContext, modulos.PaisControlador.BuscarYFiltrar);
        if (Pagina.SesionExpirada(modulos, this) is { } alLogin)
        {
            return alLogin;
        }
        HttpContext.Items["regiones"] = modulos.Busqueda.Regiones;
        return Pagina.Mostrar(this, "buscar");
    }
}
