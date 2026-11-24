using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// «Consultar país en la API» (ECUD3): busca en REST Countries a través del servidor y guarda el
/// país elegido. Cada resultado indica si ya está guardado.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class ConsultaApiPantalla : PantallaBase, ConsultaApiVista
{
    private readonly PaisAdaptador adaptador;
    private ApiControlador? controlador;
    private List<Pais>? resultados;
    private volatile Pais? seleccionado;

    public ConsultaApiPantalla()
    {
        InitializeComponent();
        Preparar(AvisoPantalla);
        adaptador = new PaisAdaptador(ListaApi, true)
        {
            AlTocar = (pais, _) => Elegir(pais),
            AlGuardar = pais =>
            {
                Elegir(pais);
                if (controlador != null)
                {
                    Ejecutar(controlador.Guardar);
                }
            }
        };
        BotonConsultar.Clicked += (_, _) => Consultar();
        CampoNombre.Entrada.Completed += (_, _) => Consultar();
    }

    internal void EstablecerControlador(ApiControlador apiControlador)
    {
        controlador = apiControlador;
    }

    private void Consultar()
    {
        if (controlador != null)
        {
            Ejecutar(controlador.Consultar);
        }
    }

    private void Elegir(Pais pais)
    {
        seleccionado = pais;
        adaptador.Seleccionar(pais);
    }

    // ------------------------------------------------------------------ ConsultaApiVista

    public string? LeerNombre()
    {
        return Hilos.EnPrincipalYEsperar(() => CampoNombre.Texto);
    }

    public void MostrarResultados(List<Pais> paises)
    {
        Hilos.EnPrincipal(() =>
        {
            resultados = paises;
            if (seleccionado != null && (paises == null || !paises.Contains(seleccionado)))
            {
                seleccionado = null;
            }
            adaptador.SetPaises(resultados);
        });
    }

    public Pais? LeerSeleccion() => seleccionado;
}
