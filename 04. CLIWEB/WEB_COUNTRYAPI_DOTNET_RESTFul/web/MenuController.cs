using ec.edu.monster.filtros;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.web;

/// <summary>
/// Menú principal con una tarjeta por opción, en el orden del ECUD1. La raíz del sitio también
/// lleva aquí (el index.jsp de Java); sin sesión, AuthFilter lleva al inicio de sesión.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[AuthFilter]
public class MenuController : Controller
{
    [HttpGet("menu")]
    public IActionResult Mostrar()
    {
        return Pagina.Mostrar(this, "menu");
    }

    [HttpGet("/")]
    public IActionResult Raiz()
    {
        return Redirect("/menu");
    }
}
