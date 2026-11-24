using ec.edu.monster.filtros;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.web;

/// <summary>Reportes (ECUD6): se generan en cada GET.</summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[AuthFilter]
[Route("reportes")]
public class ReportesController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Mostrar()
    {
        ModulosSesion modulos = ModulosSesion.De(HttpContext);
        await modulos.Ejecutar(HttpContext, modulos.ReporteControlador.Generar);
        return Pagina.SesionExpirada(modulos, this) ?? Pagina.Mostrar(this, "reportes");
    }
}
