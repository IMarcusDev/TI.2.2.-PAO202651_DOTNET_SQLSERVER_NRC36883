using System.Windows.Controls;
using System.Windows.Input;
using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Inicio de sesión (ECUD1): usuario, clave oculta y botón «Ingresar» (también con Enter).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class LoginPanel : UserControl, LoginVista
{
    private readonly VentanaPrincipal ventana;
    private Action accionIngresar = () => { };

    public LoginPanel(VentanaPrincipal ventana)
    {
        this.ventana = ventana;
        InitializeComponent();
        Ingresar.Click += (_, _) => accionIngresar();
        Loaded += (_, _) => Usuario.Focus();
    }

    /// <summary>Conecta el botón «Ingresar» y la tecla Enter con el controlador.</summary>
    public void EstablecerControlador(LoginControlador controlador)
    {
        accionIngresar = () =>
        {
            AlertaLogin.Ocultar();
            ventana.Ejecutar(controlador.IniciarSesion, this);
        };
    }

    private void AlPresionarEnUsuario(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Clave.Focus();
        }
    }

    private void AlPresionarEnClave(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            accionIngresar();
        }
    }

    public void MostrarFormulario()
    {
        Usuario.Text = "";
        Clave.Password = "";
        AlertaLogin.Ocultar();
        Usuario.Focus();
    }

    public LoginRequest LeerCredenciales()
    {
        return new LoginRequest(Usuario.Text, Clave.Password);
    }

    public void LimpiarClave()
    {
        Clave.Password = "";
        Clave.Focus();
    }

    public void MostrarMensaje(string? mensaje)
    {
        AlertaLogin.Exito(mensaje);
    }

    public void MostrarError(string? mensaje)
    {
        AlertaLogin.Error(mensaje);
    }

    public void IrAInicioSesion()
    {
        // Ya está en el inicio de sesión.
    }
}
