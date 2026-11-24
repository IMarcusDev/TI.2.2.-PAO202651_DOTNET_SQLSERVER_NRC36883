using System.Diagnostics;
using ec.edu.monster.servicios;

namespace ec.edu.monster.vista;

/// <summary>
/// Pantalla de texto (equivale al PanelBase de escritorio): implementa la parte común de
/// <see cref="VistaBase"/> y ejecuta las acciones del controlador sin que un error inesperado
/// cierre el programa.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public abstract class PantallaConsola : VistaBase
{
    internal const string OPCION_NO_VALIDA = "Opción no válida";
    internal const string ERROR_INESPERADO = ClienteRest.RESPUESTA_NO_VALIDA;

    protected readonly Consola consola;
    protected readonly SesionServicio sesion;

    protected PantallaConsola(Consola consola, SesionServicio sesion)
    {
        this.consola = consola;
        this.sesion = sesion;
    }

    public void MostrarMensaje(string? mensaje)
    {
        consola.Mensaje(mensaje);
    }

    public void MostrarError(string? mensaje)
    {
        consola.Error(mensaje);
    }

    /// <summary>
    /// El controlador ya cerró la sesión: los ciclos de las pantallas terminan porque
    /// <c>sesion.EstaActiva()</c> es falso y el programa vuelve al inicio de sesión.
    /// </summary>
    public void IrAInicioSesion()
    {
        // Nada que hacer aquí: el regreso lo hace el ciclo principal del programa.
    }

    /// <summary>Ejecuta una acción del controlador; un error inesperado se muestra y el programa sigue.</summary>
    protected void Ejecutar(Func<Task> accion)
    {
        Ejecutar(consola, accion);
    }

    /// <summary>
    /// Igual que <see cref="Ejecutar(Func{Task})"/>, para quien no es una pantalla (Program).
    /// La consola no tiene contexto de sincronización, así que esperar la tarea aquí es seguro.
    /// </summary>
    public static void Ejecutar(Consola consola, Func<Task> accion)
    {
        try
        {
            accion().GetAwaiter().GetResult();
        }
        catch (Consola.Salida)
        {
            throw;
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error inesperado en una acción: " + e);
            consola.Error(ERROR_INESPERADO);
        }
    }
}
