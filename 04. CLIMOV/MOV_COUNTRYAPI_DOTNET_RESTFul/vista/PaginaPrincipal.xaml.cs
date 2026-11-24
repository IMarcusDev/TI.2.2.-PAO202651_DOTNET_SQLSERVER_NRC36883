using System.Diagnostics;
using ec.edu.monster.controlador;
using ec.edu.monster.servicios;

namespace ec.edu.monster.vista;

/// <summary>
/// Página única de la app (equivale a PrincipalActivity de Android y a VentanaPrincipal de
/// escritorio). Implementa <see cref="MenuVista"/>, arma los controladores con sus vistas y
/// muestra una pantalla a la vez: inicio de sesión, menú de tarjetas o el módulo elegido.
///
/// Cada acción del controlador corre fuera del hilo principal con la barra de progreso visible y
/// la pantalla bloqueada; un error inesperado se muestra en la pantalla y no cierra la app.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class PaginaPrincipal : ContentPage, MenuVista
{
    internal const string LOGIN = "login";
    internal const string MENU = "menu";
    internal const string IMPORTAR = "importar";
    internal const string CONSULTAR = "consultar";
    internal const string PAISES = "paises";
    internal const string BUSCAR = "buscar";
    internal const string REPORTES = "reportes";
    internal const string UNIVERSIDADES = "universidades";

    private const string TITULO_APP = "CountryAPI";
    private const string ERROR_INESPERADO = ClienteRest.RESPUESTA_NO_VALIDA;

    private readonly Dictionary<string, PantallaBase> pantallas = [];
    private readonly Dictionary<string, Func<Task>> accionesAlEntrar = [];
    private readonly Dictionary<string, string> titulos = [];
    private readonly LoginPantalla login;
    private readonly MenuPantalla menu;
    private readonly LoginControlador loginControlador;

    private bool ocupado;
    private string pantallaActual = LOGIN;
    private Action? aceptarDialogo;
    private Action? cancelarDialogo;

    public PaginaPrincipal(AplicacionCountryApi app)
    {
        InitializeComponent();
        IndicadorBarra.EstablecerComprobador(app.Cliente.ServidorDisponible);

        // Crea cada pantalla y la conecta con su controlador, igual que App.construir en escritorio.
        login = Pantalla(LOGIN, new LoginPantalla(), TITULO_APP);
        menu = Pantalla(MENU, new MenuPantalla(), TITULO_APP);
        ImportacionPantalla importacion = Pantalla(IMPORTAR, new ImportacionPantalla(), "Importar países");
        ConsultaApiPantalla consulta = Pantalla(CONSULTAR, new ConsultaApiPantalla(), "Consultar país en la API");
        PaisPantalla paises = Pantalla(PAISES, new PaisPantalla(), "Países");
        BusquedaPantalla busqueda = Pantalla(BUSCAR, new BusquedaPantalla(), "Buscar y filtrar");
        ReportePantalla reportes = Pantalla(REPORTES, new ReportePantalla(), "Reportes");
        UniversidadPantalla universidades = Pantalla(UNIVERSIDADES, new UniversidadPantalla(), "Universidades");

        // Inicio y cierre de sesión
        loginControlador = new LoginControlador(login, this, app.LoginServicio, app.Sesion);
        login.EstablecerControlador(loginControlador, app);

        // Países: CRUD, búsqueda, importación y consulta en la API
        var apiControlador = new ApiControlador(consulta, importacion, app.PaisServicio, app.Sesion);
        importacion.EstablecerControlador(apiControlador);
        consulta.EstablecerControlador(apiControlador);
        var paisControlador = new PaisControlador(paises, busqueda, app.PaisServicio, app.Sesion);
        paises.EstablecerControlador(paisControlador);
        busqueda.EstablecerControlador(paisControlador);

        // Reportes y universidades
        var reporteControlador = new ReporteControlador(reportes, app.ReporteServicio, app.Sesion);
        reportes.EstablecerControlador(reporteControlador);
        var universidadControlador = new UniversidadControlador(universidades, app.UniversidadServicio, app.Sesion);
        universidades.EstablecerControlador(universidadControlador);

        // Lo que se consulta al entrar a cada pantalla (como en escritorio)
        accionesAlEntrar[PAISES] = paisControlador.Listar;
        accionesAlEntrar[BUSCAR] = paisControlador.BuscarYFiltrar;
        accionesAlEntrar[REPORTES] = reporteControlador.Generar;
        accionesAlEntrar[UNIVERSIDADES] = universidadControlador.Listar;

        IrAInicioSesion(null);
    }

    private T Pantalla<T>(string etiqueta, T pantalla, string titulo) where T : PantallaBase
    {
        pantalla.Principal = this;
        pantalla.IsVisible = false;
        Contenedor.Add(pantalla);
        pantallas[etiqueta] = pantalla;
        titulos[etiqueta] = titulo;
        return pantalla;
    }

    // ------------------------------------------------------------------ navegación

    /// <summary>
    /// Navegación pedida por el usuario (tarjeta del menú, flecha o «atrás»): se ignora mientras
    /// se procesa una petición.
    /// </summary>
    internal void Mostrar(string etiqueta)
    {
        if (!ocupado)
        {
            CambiarA(etiqueta);
        }
    }

    /// <summary>Muestra una pantalla, borra su aviso anterior y ejecuta lo que corresponde al entrar.</summary>
    private void CambiarA(string etiqueta)
    {
        if (!pantallas.TryGetValue(etiqueta, out PantallaBase? destino))
        {
            return;
        }
        OcultarTeclado();
        foreach (PantallaBase pantalla in pantallas.Values)
        {
            pantalla.IsVisible = pantalla == destino;
        }
        pantallaActual = etiqueta;

        bool enModulo = etiqueta != LOGIN && etiqueta != MENU;
        TituloBarra.Text = titulos[etiqueta];
        BotonVolver.IsVisible = enModulo;
        if (!enModulo)
        {
            return;
        }
        destino.LimpiarAviso();
        if (accionesAlEntrar.TryGetValue(etiqueta, out Func<Task>? accion))
        {
            Ejecutar(accion, destino);
        }
    }

    /// <summary>Vuelve al inicio de sesión mostrando, si lo hay, el motivo (por ejemplo, sesión expirada).</summary>
    internal void IrAInicioSesion(string? mensaje)
    {
        Encabezado.IsVisible = false;
        SubtituloBarra.Text = null;
        CambiarA(LOGIN);
        login.MostrarFormulario();
        if (mensaje != null)
        {
            login.MostrarError(mensaje);
        }
    }

    /// <summary>Opción «Cerrar sesión» de la barra y del menú.</summary>
    internal void CerrarSesion()
    {
        Ejecutar(loginControlador.CerrarSesion, menu);
    }

    private void AlVolver(object? origen, EventArgs e) => Mostrar(MENU);

    private void AlCerrarSesion(object? origen, EventArgs e) => CerrarSesion();

    /// <summary>
    /// Botón «atrás» de Android: cierra el diálogo abierto; en un módulo vuelve al menú; en el
    /// inicio de sesión o el menú deja la app en segundo plano sin cerrar la sesión.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        if (CapaDialogo.IsVisible)
        {
            cancelarDialogo?.Invoke();
            return true;
        }
        if (ocupado)
        {
            return true;
        }
        if (pantallaActual == LOGIN || pantallaActual == MENU)
        {
#if ANDROID
            Platform.CurrentActivity?.MoveTaskToBack(true);
#endif
        }
        else
        {
            Mostrar(MENU);
        }
        return true;
    }

    // ------------------------------------------------------------------ MenuVista

    public void MostrarMenu(string? nombre)
    {
        Hilos.EnPrincipal(() =>
        {
            Encabezado.IsVisible = true;
            SubtituloBarra.Text = nombre;
            menu.LimpiarAviso();
            menu.Saludar(nombre);
            CambiarA(MENU);
        });
    }

    /// <summary>En el móvil cada opción del menú es una tarjeta; no hay opción que leer.</summary>
    public int LeerOpcion() => -1;

    public void MostrarMensaje(string? mensaje) => menu.MostrarMensaje(mensaje);

    public void MostrarError(string? mensaje) => menu.MostrarError(mensaje);

    public void IrAInicioSesion() => Hilos.EnPrincipal(() => IrAInicioSesion(null));

    // ------------------------------------------------------------------ ejecución

    /// <summary>Ejecuta una acción del controlador en segundo plano; si ya hay una en curso, la ignora.</summary>
    internal void Ejecutar(Func<Task> accion, VistaBase vistaError)
    {
        if (ocupado)
        {
            return;
        }
        OcultarTeclado();
        SetOcupado(true);
        Task.Run(async () =>
        {
            try
            {
                await accion().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error inesperado en una acción: " + e);
                vistaError.MostrarError(ERROR_INESPERADO);
            }
            finally
            {
                Hilos.EnPrincipal(() => SetOcupado(false));
            }
        });
    }

    /// <summary>Mientras un diálogo espera al usuario se ocultan la barra de progreso y el bloqueo.</summary>
    internal void EsperandoUsuario(bool esperando)
    {
        if (ocupado)
        {
            Progreso.Ocultar(esperando);
            Bloqueo.IsVisible = !esperando;
        }
    }

    private void SetOcupado(bool valor)
    {
        ocupado = valor;
        Bloqueo.IsVisible = valor;
        if (valor)
        {
            Progreso.Ocultar(false);
            Progreso.Iniciar();
        }
        else
        {
            Progreso.Detener();
        }
    }

    private static void OcultarTeclado()
    {
#if ANDROID
        Android.App.Activity? actividad = Platform.CurrentActivity;
        Android.Views.View? foco = actividad?.CurrentFocus;
        if (actividad != null && foco != null)
        {
            var teclado = (Android.Views.InputMethods.InputMethodManager?)actividad.GetSystemService(Android.Content.Context.InputMethodService);
            teclado?.HideSoftInputFromWindow(foco.WindowToken, 0);
            foco.ClearFocus();
        }
#endif
    }

    // ------------------------------------------------------------------ diálogos

    /// <summary>
    /// Abre un diálogo sobre la pantalla (solo en el hilo principal).
    /// </summary>
    /// <param name="titulo">título del diálogo</param>
    /// <param name="mensaje">texto bajo el título, o null</param>
    /// <param name="contenido">controles del diálogo (un formulario), o null</param>
    /// <param name="textoAceptar">botón de la derecha</param>
    /// <param name="textoCancelar">botón de la izquierda</param>
    /// <param name="alAceptar">devuelve true si el diálogo se cierra (false: se queda abierto, por ejemplo con un error)</param>
    /// <param name="alCancelar">al cancelar, tocar fuera o pulsar «atrás»</param>
    internal void MostrarDialogo(string titulo, string? mensaje, View? contenido, string textoAceptar,
        string textoCancelar, Func<bool> alAceptar, Action alCancelar)
    {
        OcultarTeclado();
        TituloDialogo.Text = titulo;
        MensajeDialogo.Text = mensaje;
        MensajeDialogo.IsVisible = !string.IsNullOrEmpty(mensaje);
        ContenidoDialogo.Content = contenido;
        ContenidoDialogo.IsVisible = contenido != null;
        // La tarjeta se ajusta al contenido; si el formulario no entra en la pantalla, se desplaza.
        ContenidoDialogo.MaximumHeightRequest = Math.Max(160, Height - 260);
        BotonAceptarDialogo.Text = textoAceptar;
        BotonCancelarDialogo.Text = textoCancelar;
        aceptarDialogo = () =>
        {
            if (alAceptar())
            {
                CerrarDialogo();
            }
        };
        cancelarDialogo = () =>
        {
            CerrarDialogo();
            alCancelar();
        };
        CapaDialogo.IsVisible = true;
    }

    private void CerrarDialogo()
    {
        OcultarTeclado();
        CapaDialogo.IsVisible = false;
        ContenidoDialogo.Content = null;
        aceptarDialogo = null;
        cancelarDialogo = null;
    }

    private void AlAceptarDialogo(object? origen, EventArgs e) => aceptarDialogo?.Invoke();

    private void AlCancelarDialogo(object? origen, EventArgs e) => cancelarDialogo?.Invoke();

    private void AlTocarFueraDelDialogo(object? origen, TappedEventArgs e) => cancelarDialogo?.Invoke();
}
