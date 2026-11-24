using ec.edu.monster.vista;

namespace ec.edu.monster;

/// <summary>
/// Aplicación MAUI: una sola ventana con la página principal (equivale a la única actividad del
/// cliente Android). El modelo (cliente, sesión y servicios) se crea una sola vez.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class App : Application
{
    private readonly AplicacionCountryApi modelo;

    public App(AplicacionCountryApi modelo)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        this.modelo = modelo;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new PaginaPrincipal(modelo)) { Title = "CountryAPI" };
    }
}
