using ec.edu.monster.config;
using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Inicio de sesión (ECUD1). Además muestra a qué servidor se conecta la app y permite
/// cambiar la dirección sin recompilar.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class LoginPantalla : PantallaBase, LoginVista
{
    internal const string SERVIDOR_AYUDA = "Emulador: http://10.0.2.2:5088/api · Celular: use la IP del equipo del servidor.";
    internal const string SERVIDOR_INVALIDO = "Ingrese una dirección que empiece con http:// o https://";

    private LoginControlador? controlador;
    private AplicacionCountryApi? app;

    public LoginPantalla()
    {
        InitializeComponent();
        Preparar(AvisoPantalla);
        BotonIngresar.Clicked += (_, _) => Ingresar();
        CampoUsuario.Entrada.Completed += (_, _) => CampoClave.Entrada.Focus();
        CampoClave.Entrada.Completed += (_, _) => Ingresar();
        BotonServidor.Clicked += (_, _) => PedirServidor();
    }

    internal void EstablecerControlador(LoginControlador loginControlador, AplicacionCountryApi aplicacion)
    {
        controlador = loginControlador;
        app = aplicacion;
        TextoServidor.Text = app.Cliente.UrlBase;
        IndicadorLogin.EstablecerComprobador(app.Cliente.ServidorDisponible);
    }

    private void Ingresar()
    {
        if (controlador != null)
        {
            Ejecutar(controlador.IniciarSesion);
        }
    }

    /// <summary>Diálogo para escribir la dirección del servidor (por ejemplo, la IP del equipo en la red).</summary>
    private void PedirServidor()
    {
        if (app == null || Principal == null)
        {
            return;
        }
        var campo = new Campo { Etiqueta = "Servidor", Teclado = Keyboard.Url, Texto = app.Cliente.UrlBase };
        var ayuda = new Label { Text = SERVIDOR_AYUDA, FontSize = 12, TextColor = Colores.De("TextoSuave"), Margin = new Thickness(2, 4, 0, 0) };
        var contenido = new VerticalStackLayout { Children = { campo, ayuda } };
        Principal.MostrarDialogo("Cambiar servidor", null, contenido, "Guardar", "Cancelar", () =>
        {
            string url = campo.Texto;
            if (!ConfigServidor.EsUrlValida(url))
            {
                // Como el TextInputLayout de Android: el error reemplaza la ayuda bajo el campo.
                ayuda.Text = SERVIDOR_INVALIDO;
                ayuda.TextColor = Colores.De("Rojo");
                return false;
            }
            string guardada = app.CambiarServidor(url);
            TextoServidor.Text = guardada;
            IndicadorLogin.ComprobarAhora();
            MostrarMensaje("Servidor actualizado: " + guardada);
            return true;
        }, () => { });
    }

    // ------------------------------------------------------------------ LoginVista

    public void MostrarFormulario()
    {
        Hilos.EnPrincipal(() =>
        {
            LimpiarAviso();
            CampoUsuario.Texto = "";
            CampoClave.Texto = "";
            CampoUsuario.Entrada.Focus();
        });
    }

    public LoginRequest LeerCredenciales()
    {
        return Hilos.EnPrincipalYEsperar(() => new LoginRequest(CampoUsuario.Texto, CampoClave.Texto));
    }

    public void LimpiarClave()
    {
        Hilos.EnPrincipal(() => CampoClave.Texto = "");
    }

    /// <summary>Ya está en el inicio de sesión.</summary>
    public override void IrAInicioSesion()
    {
    }
}
