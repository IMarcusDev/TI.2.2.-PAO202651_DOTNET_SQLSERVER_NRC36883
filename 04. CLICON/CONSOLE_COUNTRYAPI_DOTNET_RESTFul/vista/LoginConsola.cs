using ec.edu.monster.modelos;
using ec.edu.monster.servicios;

namespace ec.edu.monster.vista;

/// <summary>
/// Inicio de sesión (ECUD1). La clave no se muestra al escribirla cuando hay una terminal real.
/// Escribir 0 como usuario termina el programa.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class LoginConsola : PantallaConsola, LoginVista
{
    internal const string SALIR = "0";

    private bool formularioMostrado;

    public LoginConsola(Consola consola, SesionServicio sesion) : base(consola, sesion)
    {
    }

    public void MostrarFormulario()
    {
        formularioMostrado = true;
        consola.Titulo("CountryAPI · Iniciar sesión");
        consola.Ayuda("Consumo de APIs públicas · Escriba 0 como usuario para salir.");
    }

    public LoginRequest LeerCredenciales()
    {
        string usuario = consola.Leer("Usuario");
        if (usuario == SALIR)
        {
            throw new Consola.Salida();
        }
        return new LoginRequest(usuario, consola.LeerClave("Clave"));
    }

    /// <summary>En consola la clave no queda escrita en ningún campo: el próximo intento la vuelve a pedir.</summary>
    public void LimpiarClave()
    {
    }

    /// <summary>
    /// True si el formulario ya se mostró desde la última consulta (por ejemplo, al cerrar
    /// sesión lo muestra el controlador); así Program no lo repite.
    /// </summary>
    public bool TomarFormularioMostrado()
    {
        bool mostrado = formularioMostrado;
        formularioMostrado = false;
        return mostrado;
    }
}
