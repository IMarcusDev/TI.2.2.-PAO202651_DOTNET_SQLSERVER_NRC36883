using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.web;

/// <summary>
/// Cierre de sesión (ECUD1): avisa al servidor para que elimine el token, cierra la sesión y
/// vuelve al inicio de sesión, aunque el servidor no responda.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[Route("logout")]
public class LogoutController : Controller
{
    [HttpPost]
    public async Task<IActionResult> Salir()
    {
        ModulosSesion? modulos = ModulosSesion.Existente(HttpContext);
        if (modulos != null)
        {
            await modulos.Ejecutar(HttpContext, modulos.LoginControlador.CerrarSesion);
        }
        return Pagina.AlInicioSesion(this);
    }
}
