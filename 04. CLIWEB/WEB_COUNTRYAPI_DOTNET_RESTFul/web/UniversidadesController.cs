using ec.edu.monster.filtros;
using ec.edu.monster.servicios;
using ec.edu.monster.vista;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.web;

/// <summary>
/// Universidades (ECUD10). GET muestra las guardadas del país (?pais=, por defecto Ecuador);
/// POST con accion=importar importa las de ese país; con accion=listar solo cambia de país. En
/// los dos casos redirige a la lista.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[AuthFilter]
[Route("universidades")]
public class UniversidadesController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Mostrar()
    {
        ModulosSesion modulos = ModulosSesion.De(HttpContext);
        await modulos.Ejecutar(HttpContext, modulos.UniversidadControlador.Listar);
        return Pagina.SesionExpirada(modulos, this) ?? Pagina.Mostrar(this, "universidades");
    }

    [HttpPost]
    public async Task<IActionResult> Enviar()
    {
        await Request.ReadFormAsync();
        if (Request.Form["accion"] == "listar")
        {
            // «Ver guardadas»: solo cambia de país (la lista la carga el GET).
            string pais = Request.Form[UniversidadVistaWeb.PAIS].ToString().Trim();
            return Pagina.Redirigir(this, "/universidades?pais=" + ClienteRest.Codificar(pais));
        }
        ModulosSesion modulos = ModulosSesion.De(HttpContext);
        await modulos.Ejecutar(HttpContext, modulos.UniversidadControlador.Importar);
        if (Pagina.SesionExpirada(modulos, this) is { } alLogin)
        {
            return alLogin;
        }
        string elegido = HttpContext.Items[UniversidadVistaWeb.PAIS]?.ToString() ?? "";
        return Pagina.Redirigir(this, "/universidades?pais=" + ClienteRest.Codificar(elegido));
    }
}
