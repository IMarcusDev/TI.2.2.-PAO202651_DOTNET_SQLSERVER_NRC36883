using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Formulario de inicio de sesión (ECUD1).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface LoginVista : VistaBase
{
    /// <summary>Muestra el formulario vacío, listo para escribir.</summary>
    void MostrarFormulario();

    /// <summary>Usuario y clave escritos (pueden venir vacíos; los valida el controlador).</summary>
    LoginRequest LeerCredenciales();

    /// <summary>Borra la clave después de unas credenciales incorrectas.</summary>
    void LimpiarClave();
}
