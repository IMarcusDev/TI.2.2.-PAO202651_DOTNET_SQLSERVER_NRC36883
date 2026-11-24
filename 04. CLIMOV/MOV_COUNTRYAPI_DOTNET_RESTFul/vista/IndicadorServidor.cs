using Microsoft.Maui.Controls.Shapes;

namespace ec.edu.monster.vista;

/// <summary>
/// Punto verde o rojo con el texto «Servidor disponible» / «Servidor no disponible». Mientras
/// está visible consulta el servidor cada 10 segundos, fuera del hilo principal.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class IndicadorServidor : ContentView
{
    public const string COMPROBANDO = "Comprobando el servidor…";
    public const string DISPONIBLE = "Servidor disponible";
    public const string NO_DISPONIBLE = "Servidor no disponible";

    private static readonly TimeSpan INTERVALO = TimeSpan.FromSeconds(10);
    /// <summary>Una consulta a la vez para todos los indicadores: no se acumulan.</summary>
    private static readonly SemaphoreSlim HILO = new(1, 1);

    private readonly Ellipse punto;
    private readonly Label texto;
    private readonly List<VisualElement> ancestros = [];
    private IDispatcherTimer? temporizador;
    private Func<Task<bool>>? comprobador;
    private bool cargado;
    private bool activo;

    /// <summary>True cuando va sobre la barra azul (texto blanco).</summary>
    public bool SobreAzul { get; set; }

    public IndicadorServidor()
    {
        punto = new Ellipse { WidthRequest = 10, HeightRequest = 10, VerticalOptions = LayoutOptions.Center };
        texto = new Label { FontSize = 13, VerticalOptions = LayoutOptions.Center };
        Content = new HorizontalStackLayout { Spacing = 8, Children = { punto, texto } };
        Loaded += (_, _) => AlCargar();
        Unloaded += (_, _) => AlDescargar();
        Pintar(null);
    }

    /// <summary>Función que responde si el servidor está disponible (se llama fuera del hilo principal).</summary>
    public void EstablecerComprobador(Func<Task<bool>> funcion)
    {
        comprobador = funcion;
        ActualizarCiclo();
    }

    /// <summary>Consulta de inmediato (por ejemplo, después de cambiar la dirección del servidor).</summary>
    public void ComprobarAhora()
    {
        temporizador?.Stop();
        if (!activo || comprobador == null)
        {
            return;
        }
        Func<Task<bool>> consulta = comprobador;
        Task.Run(async () =>
        {
            bool disponible;
            await HILO.WaitAsync().ConfigureAwait(false);
            try
            {
                disponible = await consulta().ConfigureAwait(false);
            }
            catch (Exception)
            {
                disponible = false;
            }
            finally
            {
                HILO.Release();
            }
            Hilos.EnPrincipal(() =>
            {
                Pintar(disponible);
                if (activo)
                {
                    temporizador?.Start();
                }
            });
        });
    }

    private void AlCargar()
    {
        cargado = true;
        if (temporizador == null && Dispatcher != null)
        {
            temporizador = Dispatcher.CreateTimer();
            temporizador.Interval = INTERVALO;
            temporizador.IsRepeating = false;
            temporizador.Tick += (_, _) => ComprobarAhora();
        }
        // Se escucha la visibilidad propia y la de cada contenedor: solo consulta mientras se ve.
        for (Element? e = this; e != null; e = e.Parent)
        {
            if (e is VisualElement visual)
            {
                visual.PropertyChanged += AlCambiarVisibilidad;
                ancestros.Add(visual);
            }
        }
        ActualizarCiclo();
    }

    private void AlDescargar()
    {
        cargado = false;
        foreach (VisualElement visual in ancestros)
        {
            visual.PropertyChanged -= AlCambiarVisibilidad;
        }
        ancestros.Clear();
        ActualizarCiclo();
    }

    private void AlCambiarVisibilidad(object? origen, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsVisible))
        {
            ActualizarCiclo();
        }
    }

    /// <summary>Consulta solo mientras se ve en pantalla, para no gastar peticiones.</summary>
    private void ActualizarCiclo()
    {
        bool debeEstarActivo = comprobador != null && cargado && SeVe();
        if (debeEstarActivo && !activo)
        {
            activo = true;
            Pintar(null);
            ComprobarAhora();
        }
        else if (!debeEstarActivo && activo)
        {
            activo = false;
            temporizador?.Stop();
        }
    }

    private bool SeVe()
    {
        for (Element? e = this; e != null; e = e.Parent)
        {
            if (e is VisualElement { IsVisible: false })
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>null: comprobando; true: disponible; false: no disponible.</summary>
    private void Pintar(bool? disponible)
    {
        texto.TextColor = Colores.De(SobreAzul ? "Blanco" : "TextoSuave");
        string color;
        if (disponible == null)
        {
            texto.Text = COMPROBANDO;
            color = SobreAzul ? "TextoSobreAzul" : "Borde";
        }
        else if (disponible.Value)
        {
            texto.Text = DISPONIBLE;
            color = "Verde";
        }
        else
        {
            texto.Text = NO_DISPONIBLE;
            color = SobreAzul ? "RojoClaro" : "Rojo";
        }
        punto.Fill = new SolidColorBrush(Colores.De(color));
    }
}
