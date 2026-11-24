using ec.edu.monster.filtros;
using ec.edu.monster.vista;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.web;

/// <summary>
/// «Consultar país en la API» (ECUD3). GET con ?nombre consulta REST Countries a través del
/// servidor; sin nombre muestra la última consulta del usuario (así, después de «Guardar», la
/// página no gasta otra consulta). POST guarda el país de la fila elegida.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[AuthFilter]
[Route("consultar")]
public class ConsultarController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Mostrar()
    {
        ModulosSesion modulos = ModulosSesion.De(HttpContext);
        if (Request.Query.ContainsKey("nombre"))
        {
            await modulos.Ejecutar(HttpContext, modulos.ApiControlador.Consultar);
            if (Pagina.SesionExpirada(modulos, this) is { } alLogin)
            {
                return alLogin;
            }
        }
        ConsultaApiVistaWeb vista = modulos.Consulta;
        HttpContext.Items[ConsultaApiVistaWeb.RESULTADOS] = vista.Resultados;
        HttpContext.Items["nombre"] = vista.UltimoNombre;
        return Pagina.Mostrar(this, "consultar");
    }

    [HttpPost]
    public async Task<IActionResult> Guardar()
    {
        ModulosSesion modulos = ModulosSesion.De(HttpContext);
        await modulos.Ejecutar(HttpContext, modulos.ApiControlador.Guardar);
        return Pagina.SesionExpirada(modulos, this) ?? Pagina.Redirigir(this, "/consultar");
    }
}
