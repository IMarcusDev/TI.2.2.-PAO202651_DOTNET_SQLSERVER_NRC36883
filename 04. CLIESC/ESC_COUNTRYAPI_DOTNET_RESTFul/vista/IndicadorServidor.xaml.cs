using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ec.edu.monster.vista;

/// <summary>
/// Indicador de la barra inferior: verde si el servidor responde, rojo si no. Se revisa al abrir
/// y cada 10 segundos, sin bloquear la interfaz.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class IndicadorServidor : UserControl
{
    private const int INTERVALO_SEGUNDOS = 10;

    private Func<Task<bool>> servidorDisponible = () => Task.FromResult(false);
    private bool revisando;

    public IndicadorServidor()
    {
        InitializeComponent();
    }

    public void Iniciar(Func<Task<bool>> disponible)
    {
        servidorDisponible = disponible;
        Revisar();
        var temporizador = new DispatcherTimer { Interval = TimeSpan.FromSeconds(INTERVALO_SEGUNDOS) };
        temporizador.Tick += (_, _) => Revisar();
        temporizador.Start();
    }

    private async void Revisar()
    {
        if (revisando)
        {
            return;
        }
        revisando = true;
        bool disponible;
        try
        {
            disponible = await servidorDisponible();
        }
        catch (Exception)
        {
            disponible = false;
        }
        revisando = false;
        Mostrar(disponible);
    }

    private void Mostrar(bool disponible)
    {
        Punto.Fill = (Brush)FindResource(disponible ? "Verde" : "Rojo");
        Texto.Text = disponible ? "Servidor disponible" : "Servidor no disponible";
    }
}
