using System.Windows.Input;
using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// «Consultar país en la API» (ECUD3): busca por nombre en REST Countries (a través del servidor),
/// muestra los resultados y si cada país ya está guardado, y permite guardar el elegido.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class ConsultaApiPanel : PanelBase, ConsultaApiVista
{
    public ConsultaApiPanel(VentanaPrincipal ventana)
    {
        InitializeComponent();
        Preparar(ventana, AlertaPantalla);
    }

    public void EstablecerControlador(ApiControlador controlador)
    {
        Consultar.Click += (_, _) => Ejecutar(controlador.Consultar);
        Nombre.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Ejecutar(controlador.Consultar);
            }
        };
        Guardar.Click += (_, _) => Ejecutar(controlador.Guardar);
    }

    public string? LeerNombre()
    {
        return Nombre.Text;
    }

    public void MostrarResultados(List<Pais> paises)
    {
        // Se vuelve a asignar la lista para que se vea el «Sí» del país recién guardado,
        // conservando la fila elegida.
        int fila = Tabla.SelectedIndex;
        Tabla.ItemsSource = null;
        Tabla.ItemsSource = paises;
        if (fila >= 0 && fila < paises.Count)
        {
            Tabla.SelectedIndex = fila;
        }
    }

    public Pais? LeerSeleccion()
    {
        return Tabla.SelectedItem as Pais;
    }
}
