using ec.edu.monster.filtros;
using ec.edu.monster.modelos;
using ec.edu.monster.vista;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.web;

/// <summary>
/// Gestión de países (ECUD4).
/// <list type="bullet">
///   <item>GET: lista los países (o los que coinciden con ?q=). Además, ?nuevo abre el
///         formulario vacío, ?editar=ID lo abre con los datos del país y ?eliminar=ID muestra
///         la confirmación.</item>
///   <item>POST accion=registrar|actualizar|eliminar: llama al controlador. Si funcionó,
///         redirige a la lista con el mensaje; si los datos no son válidos o el país ya
///         existe, vuelve a mostrar el formulario con lo escrito y el error.</item>
/// </list>
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[AuthFilter]
[Route("paises")]
public class PaisesController : Controller
{
    [HttpGet]
    public async Task<IActionResult> Mostrar()
    {
        ModulosSesion modulos = ModulosSesion.De(HttpContext);
        bool busqueda = Request.Query.ContainsKey("q");
        await modulos.Ejecutar(HttpContext, busqueda ? modulos.PaisControlador.Buscar : modulos.PaisControlador.Listar);
        if (Pagina.SesionExpirada(modulos, this) is { } alLogin)
        {
            return alLogin;
        }
        PaisVistaWeb vista = modulos.Paises;
        if (Request.Query.ContainsKey("nuevo"))
        {
            // Población -1 y área NaN: el formulario los muestra vacíos.
            HttpContext.Items[PaisVistaWeb.FORMULARIO] = new Pais(null, null, null, null, -1, double.NaN, null);
        }
        else if (Request.Query.ContainsKey("editar"))
        {
            Pais? pais = vista.Buscar(Request.Query["editar"]);
            if (pais != null)
            {
                HttpContext.Items[PaisVistaWeb.FORMULARIO] = pais.Copia();
            }
        }
        else if (Request.Query.ContainsKey("eliminar"))
        {
            HttpContext.Items[PaisVistaWeb.ELIMINAR] = vista.Buscar(Request.Query["eliminar"]);
        }
        return MostrarPagina();
    }

    [HttpPost]
    public async Task<IActionResult> Guardar()
    {
        ModulosSesion modulos = ModulosSesion.De(HttpContext);
        await Request.ReadFormAsync();
        Func<Task>? operacion = Request.Form["accion"].ToString() switch
        {
            "registrar" => modulos.PaisControlador.Registrar,
            "actualizar" => modulos.PaisControlador.Actualizar,
            "eliminar" => modulos.PaisControlador.Eliminar,
            _ => null
        };
        if (operacion != null)
        {
            await modulos.Ejecutar(HttpContext, operacion);
            if (Pagina.SesionExpirada(modulos, this) is { } alLogin)
            {
                return alLogin;
            }
        }
        if (HttpContext.Items[PaisVistaWeb.FORMULARIO] != null)
        {
            // Datos rechazados: el formulario se vuelve a mostrar sobre la última tabla.
            HttpContext.Items[PaisVistaWeb.PAISES] = modulos.Paises.UltimaTabla;
            return MostrarPagina();
        }
        return Pagina.Redirigir(this, "/paises");
    }

    private IActionResult MostrarPagina()
    {
        // Regiones de la lista desplegable del formulario.
        HttpContext.Items["regiones"] = Pais.REGIONES;
        return Pagina.Mostrar(this, "paises");
    }
}
