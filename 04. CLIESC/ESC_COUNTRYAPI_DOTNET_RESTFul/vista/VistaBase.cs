namespace ec.edu.monster.vista;

/// <summary>
/// Lo que toda vista sabe hacer, en cualquier tecnología (WPF, consola, Razor o MAUI).
/// Los controladores solo conocen estas interfaces, por eso se prueban sin pantalla.
/// Las interfaces se llaman como en el diagrama DC_Clientes, sin el prefijo «I».
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface VistaBase
{
    /// <summary>Muestra un mensaje de éxito o informativo (alerta verde).</summary>
    void MostrarMensaje(string? mensaje);

    /// <summary>Muestra un mensaje de error destacado (alerta roja).</summary>
    void MostrarError(string? mensaje);

    /// <summary>Vuelve al formulario de inicio de sesión (sesión cerrada o expirada).</summary>
    void IrAInicioSesion();
}
