using System.Windows.Controls;

namespace ec.edu.monster.vista;

/// <summary>
/// Pantalla WPF de la aplicación: título, descripción, alerta y contenido (cada pantalla los
/// declara en su XAML). Implementa <see cref="VistaBase"/>; las subclases implementan además la
/// interfaz de su módulo.
///
/// Las acciones de los controladores se ejecutan con <see cref="Ejecutar"/>: mientras esperan
/// al servidor la interfaz sigue respondiendo, se muestra la barra de progreso y la ventana no
/// acepta clics.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class PanelBase : UserControl, VistaBase
{
    protected VentanaPrincipal ventana = null!;
    private Alerta alerta = null!;
    private string? ultimoError;

    /// <summary>La ventana y la alerta de la pantalla; se llama después de InitializeComponent.</summary>
    protected void Preparar(VentanaPrincipal ventanaPrincipal, Alerta alertaPantalla)
    {
        ventana = ventanaPrincipal;
        alerta = alertaPantalla;
    }

    public void MostrarMensaje(string? mensaje)
    {
        alerta.Exito(mensaje);
    }

    public void MostrarError(string? mensaje)
    {
        ultimoError = mensaje;
        alerta.Error(mensaje);
    }

    public void IrAInicioSesion()
    {
        ventana.IrAInicioSesion(ultimoError);
    }

    /// <summary>Oculta la alerta (al entrar a la pantalla).</summary>
    internal void LimpiarAviso()
    {
        ultimoError = null;
        alerta.Ocultar();
    }

    /// <summary>Ejecuta una acción del controlador con el indicador de carga.</summary>
    protected void Ejecutar(Func<Task> accion)
    {
        ventana.Ejecutar(accion, this);
    }
}
