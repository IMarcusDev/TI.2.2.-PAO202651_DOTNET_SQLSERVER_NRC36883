using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>Inicio de sesión (ECUD1): lee usuario y clave del formulario de login.cshtml.</summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class LoginVistaWeb : VistaWeb, LoginVista
{
    /// <summary>El controlador MVC muestra login.cshtml; no hay nada que preparar.</summary>
    public void MostrarFormulario()
    {
    }

    public LoginRequest LeerCredenciales()
    {
        return new LoginRequest(Parametro("usuario") ?? "", ParametroCrudo("clave") ?? "");
    }

    /// <summary>login.cshtml nunca vuelve a escribir la clave en el formulario.</summary>
    public void LimpiarClave()
    {
    }
}
