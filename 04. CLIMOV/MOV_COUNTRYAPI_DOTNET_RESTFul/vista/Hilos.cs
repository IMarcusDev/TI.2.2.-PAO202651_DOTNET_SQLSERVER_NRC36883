namespace ec.edu.monster.vista;

/// <summary>
/// Paso de datos entre el hilo de la interfaz y el hilo donde corren los controladores.
///
/// Los controladores son los mismos del cliente de escritorio y llaman a la vista de forma
/// síncrona (por ejemplo <c>vista.LeerPais(...)</c> espera a que el usuario cierre el
/// formulario). Por eso se ejecutan fuera del hilo principal y la vista usa estos métodos para
/// tocar los controles en el hilo principal.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
internal static class Hilos
{
    /// <summary>Ejecuta en el hilo principal: al momento si ya está en él.</summary>
    public static void EnPrincipal(Action accion)
    {
        if (MainThread.IsMainThread)
        {
            accion();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(accion);
        }
    }

    /// <summary>Ejecuta en el hilo principal y devuelve el resultado (por ejemplo, el texto de un campo).</summary>
    public static T EnPrincipalYEsperar<T>(Func<T> lectura)
    {
        if (MainThread.IsMainThread)
        {
            return lectura();
        }
        return MainThread.InvokeOnMainThreadAsync(lectura).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Muestra algo que responde más tarde (un diálogo) y bloquea el hilo actual hasta la
    /// respuesta. Solo se cuenta la primera respuesta (botón o cierre del diálogo).
    /// </summary>
    /// <param name="mostrar">recibe la función con la que el diálogo entrega su respuesta</param>
    public static T? EsperarRespuesta<T>(Action<Action<T?>> mostrar)
    {
        if (MainThread.IsMainThread)
        {
            throw new InvalidOperationException("No se puede esperar al usuario en el hilo principal");
        }
        var respuesta = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);
        MainThread.BeginInvokeOnMainThread(() => mostrar(valor => respuesta.TrySetResult(valor)));
        return respuesta.Task.GetAwaiter().GetResult();
    }
}
