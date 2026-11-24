namespace ec.edu.monster.vista;

/// <summary>
/// Pantalla de la app (equivale al FragmentoBase de Android y al PanelBase de escritorio).
/// Implementa la parte común de <see cref="VistaBase"/>: el aviso verde o rojo de arriba y el
/// regreso al inicio de sesión.
///
/// Los controladores llaman a estos métodos desde un hilo secundario; todo cambio de controles
/// pasa por <see cref="Hilos"/>.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public abstract class PantallaBase : ContentView, VistaBase
{
    private Aviso? aviso;
    private string? avisoTexto;
    private bool avisoEsError;
    private volatile string? ultimoError;

    /// <summary>La página que muestra esta pantalla (la asigna PaginaPrincipal al armarla).</summary>
    internal PaginaPrincipal? Principal { get; set; }

    /// <summary>Indica qué aviso pinta esta pantalla (se llama después de InitializeComponent).</summary>
    protected void Preparar(Aviso avisoPantalla)
    {
        aviso = avisoPantalla;
        PintarAviso();
    }

    public virtual void MostrarMensaje(string? mensaje)
    {
        Hilos.EnPrincipal(() =>
        {
            avisoTexto = mensaje;
            avisoEsError = false;
            PintarAviso();
        });
    }

    public virtual void MostrarError(string? mensaje)
    {
        ultimoError = mensaje;
        Hilos.EnPrincipal(() =>
        {
            avisoTexto = mensaje;
            avisoEsError = true;
            PintarAviso();
        });
    }

    public virtual void IrAInicioSesion()
    {
        string? mensaje = ultimoError;
        Hilos.EnPrincipal(() => Principal?.IrAInicioSesion(mensaje));
    }

    /// <summary>Oculta el aviso (al entrar a la pantalla). Solo en el hilo principal.</summary>
    internal void LimpiarAviso()
    {
        ultimoError = null;
        avisoTexto = null;
        PintarAviso();
    }

    /// <summary>Ejecuta una acción del controlador en segundo plano con el indicador de carga.</summary>
    protected void Ejecutar(Func<Task> accion)
    {
        Principal?.Ejecutar(accion, this);
    }

    /// <summary>
    /// Muestra un diálogo y espera la respuesta del usuario (se llama desde el hilo del
    /// controlador). Mientras el diálogo está abierto se oculta la barra de progreso.
    /// </summary>
    protected T? EsperarUsuario<T>(Action<Action<T?>> mostrarDialogo)
    {
        Hilos.EnPrincipal(() => Principal?.EsperandoUsuario(true));
        try
        {
            return Hilos.EsperarRespuesta(mostrarDialogo);
        }
        finally
        {
            Hilos.EnPrincipal(() => Principal?.EsperandoUsuario(false));
        }
    }

    private void PintarAviso()
    {
        aviso?.Mostrar(avisoTexto, avisoEsError);
    }
}
