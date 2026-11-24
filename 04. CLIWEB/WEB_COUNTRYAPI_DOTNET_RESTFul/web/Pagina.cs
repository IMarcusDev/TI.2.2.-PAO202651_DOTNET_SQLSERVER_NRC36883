using ec.edu.monster.vista;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.web;

/// <summary>
/// Muestra las vistas Razor y redirige entre pantallas.
///
/// Después de un POST que funcionó se redirige con GET (patrón POST/Redirect/GET), así recargar
/// la página no repite la operación. Los avisos y el resumen de importación pasan esa
/// redirección guardados un momento en la sesión («flash»).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public static class Pagina
{
    internal const string CARPETA = "~/Views/";
    internal const string PREFIJO_FLASH = "flash.";
    private static readonly string[] FLASH = [VistaWeb.MENSAJE, VistaWeb.ERROR, ImportacionVistaWeb.RESUMEN];

    /// <summary>Dibuja Views/{nombre}.cshtml con los atributos de la petición y los avisos pendientes.</summary>
    public static IActionResult Mostrar(Controller controlador, string nombre)
    {
        HttpContext contexto = controlador.HttpContext;
        SesionWeb? sesion = Almacen(contexto).Obtener(contexto, false);
        if (sesion != null)
        {
            foreach (string clave in FLASH)
            {
                object? valor = sesion.Leer(PREFIJO_FLASH + clave);
                if (valor != null)
                {
                    if (contexto.Items[clave] == null)
                    {
                        contexto.Items[clave] = valor;
                    }
                    sesion.Quitar(PREFIJO_FLASH + clave);
                }
            }
        }
        foreach (KeyValuePair<object, object?> atributo in contexto.Items)
        {
            if (atributo.Key is string clave)
            {
                controlador.ViewData[clave] = atributo.Value;
            }
        }
        return controlador.View(CARPETA + nombre + ".cshtml");
    }

    /// <summary>Redirige (GET) a otra ruta de la aplicación llevando los avisos de esta petición.</summary>
    public static IActionResult Redirigir(Controller controlador, string ruta)
    {
        HttpContext contexto = controlador.HttpContext;
        SesionWeb? sesion = Almacen(contexto).Obtener(contexto, false);
        if (sesion != null)
        {
            foreach (string clave in FLASH)
            {
                if (contexto.Items[clave] is { } valor)
                {
                    sesion.Poner(PREFIJO_FLASH + clave, valor);
                }
            }
        }
        return controlador.Redirect(ruta);
    }

    /// <summary>
    /// Cierra la sesión y vuelve al inicio de sesión mostrando el error de esta petición (por
    /// ejemplo, «Su sesión expiró o no es válida...»).
    /// </summary>
    public static IActionResult AlInicioSesion(Controller controlador)
    {
        HttpContext contexto = controlador.HttpContext;
        object? error = contexto.Items[VistaWeb.ERROR];
        AlmacenSesiones almacen = Almacen(contexto);
        almacen.Invalidar(contexto);
        if (error != null)
        {
            almacen.Obtener(contexto, true)!.Poner(PREFIJO_FLASH + VistaWeb.ERROR, error);
        }
        return controlador.Redirect("/login");
    }

    /// <summary>Si el controlador cerró la sesión (token rechazado), la respuesta que lleva al inicio de sesión; si no, null.</summary>
    public static IActionResult? SesionExpirada(ModulosSesion modulos, Controller controlador)
    {
        return modulos.Sesion.EstaActiva() ? null : AlInicioSesion(controlador);
    }

    private static AlmacenSesiones Almacen(HttpContext contexto)
    {
        return contexto.RequestServices.GetRequiredService<AlmacenSesiones>();
    }
}
