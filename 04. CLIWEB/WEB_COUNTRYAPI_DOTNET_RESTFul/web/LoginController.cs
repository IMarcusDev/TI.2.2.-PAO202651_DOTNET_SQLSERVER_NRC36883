using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.web;

/// <summary>
/// Inicio de sesión (ECUD1). GET muestra el formulario; POST pide el token al servidor y, si
/// todo está bien, cambia el id de la sesión y abre el menú.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[Route("login")]
public class LoginController(AlmacenSesiones almacen) : Controller
{
    [HttpGet]
    public IActionResult Mostrar()
    {
        if (ModulosSesion.ConSesion(HttpContext))
        {
            return Redirect("/menu");
        }
        // Como el CsrfFilter de Java, solo GET /login crea la sesión antes de autenticarse.
        almacen.Obtener(HttpContext, true);
        return Pagina.Mostrar(this, "login");
    }

    [HttpPost]
    public async Task<IActionResult> Ingresar()
    {
        ModulosSesion modulos = ModulosSesion.De(HttpContext);
        await modulos.Ejecutar(HttpContext, modulos.LoginControlador.IniciarSesion);
        if (!modulos.Sesion.EstaActiva())
        {
            // Credenciales incorrectas o sin conexión: el formulario vuelve con el usuario escrito.
            return Pagina.Mostrar(this, "login");
        }
        // Nuevo id de sesión al autenticarse (evita la fijación de sesión).
        almacen.CambiarId(HttpContext, almacen.Obtener(HttpContext, false)!);
        return Pagina.Redirigir(this, "/menu");
    }
}
