using ec.edu.monster.modelos;
using ec.edu.monster.servicios;
using ec.edu.monster.vista;

namespace ec.edu.monster.controlador;

/// <summary>
/// Inicio y cierre de sesión (ECUD1). También abre el menú principal, porque el menú depende de
/// la sesión (MenuVista → LoginControlador en DC_Clientes).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class LoginControlador
{
    private readonly LoginVista vista;
    private readonly MenuVista menu;
    private readonly LoginServicio servicio;
    private readonly SesionServicio sesion;

    public LoginControlador(LoginVista vista, MenuVista menu, LoginServicio servicio, SesionServicio sesion)
    {
        this.vista = vista;
        this.menu = menu;
        this.servicio = servicio;
        this.sesion = sesion;
    }

    /// <summary>
    /// Valida que haya usuario y clave, pide el token al servidor y, si todo está bien, guarda la
    /// sesión y muestra el menú. Con credenciales incorrectas borra la clave; sin conexión
    /// conserva lo escrito.
    /// </summary>
    public async Task IniciarSesion()
    {
        LoginRequest credenciales = vista.LeerCredenciales();
        string? error = ValidadorDatos.ValidarCredenciales(credenciales.Usuario, credenciales.Clave);
        if (error != null)
        {
            vista.MostrarError(error);
            return;
        }
        RespuestaDTO<LoginResponse> respuesta = await servicio.Login(credenciales.Usuario!.Trim(), credenciales.Clave!);
        if (!respuesta.EsOk() || respuesta.Datos == null)
        {
            vista.MostrarError(respuesta.Mensaje);
            if (respuesta.CodigoHttp == 401)
            {
                vista.LimpiarClave();
            }
            return;
        }
        sesion.Guardar(respuesta.Datos.Token, respuesta.Datos.Nombre);
        menu.MostrarMenu(respuesta.Datos.Nombre);
    }

    /// <summary>
    /// Avisa al servidor para que elimine el token y vuelve al inicio de sesión, aunque el
    /// servidor no responda (ECUD1, flujo alternativo 4).
    /// </summary>
    public async Task CerrarSesion()
    {
        if (sesion.EstaActiva())
        {
            await servicio.Logout(sesion.ObtenerToken());
        }
        sesion.Cerrar();
        menu.IrAInicioSesion();
        vista.MostrarFormulario();
    }
}
