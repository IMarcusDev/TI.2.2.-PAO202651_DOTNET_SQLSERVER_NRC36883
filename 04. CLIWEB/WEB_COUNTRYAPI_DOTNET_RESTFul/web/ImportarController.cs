using ec.edu.monster.filtros;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.web;

/// <summary>
/// «Importar países» (ECUD2). GET muestra la pantalla (con ?confirmar, el panel de
/// confirmación); POST importa y redirige mostrando el resumen.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[AuthFilter]
[Route("importar")]
public class ImportarController : Controller
{
    [HttpGet]
    public IActionResult Mostrar()
    {
        HttpContext.Items["confirmar"] = Request.Query.ContainsKey("confirmar");
        return Pagina.Mostrar(this, "importar");
    }

    [HttpPost]
    public async Task<IActionResult> Importar()
    {
        ModulosSesion modulos = ModulosSesion.De(HttpContext);
        await modulos.Ejecutar(HttpContext, modulos.ApiControlador.Importar);
        return Pagina.SesionExpirada(modulos, this) ?? Pagina.Redirigir(this, "/importar");
    }
}
