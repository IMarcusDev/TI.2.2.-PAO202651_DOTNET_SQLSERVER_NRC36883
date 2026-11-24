using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ec.edu.monster.vista;

/// <summary>
/// Única ventana de la aplicación. Tiene dos partes: el inicio de sesión y la aplicación
/// (barra superior azul con la navegación y el usuario, la pantalla activa y la barra inferior con
/// el indicador del servidor). Implementa <see cref="MenuVista"/>.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class VentanaPrincipal : Window, MenuVista
{
    internal const string ERROR_INESPERADO = "Ocurrió un error inesperado, intente nuevamente";
    public const string MENU = "menu";

    private readonly Dictionary<string, PanelBase> paneles = [];
    private readonly Dictionary<string, Func<Task>> alAbrir = [];
    private readonly MenuPanel menu;
    private LoginPanel? login;
    private bool ocupado;

    public VentanaPrincipal(string urlServidor, Func<Task<bool>> servidorDisponible)
    {
        InitializeComponent();
        UrlServidor.Text = urlServidor;
        menu = new MenuPanel(this);
        Registrar(MENU, menu, null);
        Indicador.Iniciar(servidorDisponible);
    }

    // ------------------------------------------------------------------ armado (lo usa App)

    public void EstablecerLogin(LoginPanel panelLogin)
    {
        login = panelLogin;
        TarjetaLogin.Content = panelLogin;
        MostrarTarjetaLogin(true);
    }

    /// <summary>
    /// Agrega una pantalla con su botón en la barra superior y su tarjeta en el menú.
    /// </summary>
    /// <param name="accionAlAbrir">acción del controlador que se ejecuta al entrar (puede ser null)</param>
    public void AgregarPantalla(string clave, string nombre, string descripcion, PanelBase panel,
        Func<Task>? accionAlAbrir)
    {
        Registrar(clave, panel, accionAlAbrir);
        var boton = new Button { Content = nombre, Style = (Style)FindResource("BotonBarra") };
        boton.Click += (_, _) => Mostrar(clave);
        Navegacion.Children.Add(boton);
        menu.AgregarOpcion(nombre, descripcion, () => Mostrar(clave));
    }

    public void EstablecerAccionCerrarSesion(Func<Task> accion)
    {
        BotonCerrarSesion.Click += (_, _) => Ejecutar(accion, menu);
        menu.AgregarOpcion("Cerrar sesión", "Termina la sesión y vuelve al inicio.", () => Ejecutar(accion, menu));
    }

    private void Registrar(string clave, PanelBase panel, Func<Task>? accionAlAbrir)
    {
        paneles[clave] = panel;
        if (accionAlAbrir != null)
        {
            alAbrir[clave] = accionAlAbrir;
        }
        panel.Visibility = Visibility.Collapsed;
        Pantallas.Children.Add(panel);
    }

    // ------------------------------------------------------------------ navegación

    /// <summary>Muestra una pantalla y ejecuta su acción de entrada (por ejemplo, cargar la tabla).</summary>
    public void Mostrar(string clave)
    {
        if (ocupado)
        {
            return;
        }
        PanelBase panel = paneles[clave];
        panel.LimpiarAviso();
        MostrarPantalla(clave);
        if (alAbrir.TryGetValue(clave, out Func<Task>? accion))
        {
            Ejecutar(accion, panel);
        }
    }

    private void MostrarPantalla(string clave)
    {
        foreach (var par in paneles)
        {
            par.Value.Visibility = par.Key == clave ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void MostrarTarjetaLogin(bool enLogin)
    {
        TarjetaLogin.Visibility = enLogin ? Visibility.Visible : Visibility.Collapsed;
        TarjetaApp.Visibility = enLogin ? Visibility.Collapsed : Visibility.Visible;
    }

    private void AlElegirMarca(object sender, RoutedEventArgs e)
    {
        Mostrar(MENU);
    }

    public void MostrarMenu(string? nombre)
    {
        Usuario.Text = nombre ?? "";
        MostrarTarjetaLogin(false);
        menu.LimpiarAviso();
        menu.Saludar(nombre);
        MostrarPantalla(MENU);
    }

    public int LeerOpcion()
    {
        // En WPF cada opción del menú es un botón; no hay opción que leer.
        return -1;
    }

    public void IrAInicioSesion()
    {
        IrAInicioSesion(null);
    }

    /// <summary>Vuelve al inicio de sesión mostrando, si lo hay, el motivo (por ejemplo, sesión expirada).</summary>
    internal void IrAInicioSesion(string? mensaje)
    {
        Usuario.Text = "";
        MostrarTarjetaLogin(true);
        if (login != null)
        {
            login.MostrarFormulario();
            if (mensaje != null)
            {
                login.MostrarError(mensaje);
            }
        }
    }

    public void MostrarMensaje(string? mensaje)
    {
        menu.MostrarMensaje(mensaje);
    }

    public void MostrarError(string? mensaje)
    {
        menu.MostrarError(mensaje);
    }

    // ------------------------------------------------------------------ indicador de carga

    /// <summary>
    /// Ejecuta una acción del controlador. Mientras espera al servidor la interfaz no se congela,
    /// la barra de progreso está visible y la ventana no acepta clics; un error inesperado se
    /// muestra en la vista indicada y nunca cierra la aplicación.
    /// </summary>
    internal async void Ejecutar(Func<Task> accion, VistaBase vistaError)
    {
        if (ocupado)
        {
            return;
        }
        EstablecerOcupado(true);
        try
        {
            await accion();
        }
        catch (Exception e)
        {
            Trace.TraceError("Error inesperado en una acción: " + e);
            vistaError.MostrarError(ERROR_INESPERADO);
        }
        finally
        {
            EstablecerOcupado(false);
        }
    }

    private void EstablecerOcupado(bool valor)
    {
        ocupado = valor;
        Progreso.Visibility = valor ? Visibility.Visible : Visibility.Collapsed;
        Bloqueo.Visibility = valor ? Visibility.Visible : Visibility.Collapsed;
    }
}
