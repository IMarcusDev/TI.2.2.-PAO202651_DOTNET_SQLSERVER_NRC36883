using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Gestión de países (ECUD4): lista con búsqueda por nombre, «Nuevo» y, al tocar un país,
/// «Editar» o «Eliminar».
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class PaisPantalla : PantallaBase, PaisVista
{
    private const string EDITAR = "Editar";
    private const string ELIMINAR = "Eliminar";

    private readonly PaisAdaptador adaptador;
    private PaisControlador? controlador;
    private volatile Pais? seleccionado;
    /// <summary>Error que el controlador mostró antes de reabrir el formulario.</summary>
    private volatile string? errorFormulario;

    public PaisPantalla()
    {
        InitializeComponent();
        Preparar(AvisoPantalla);
        adaptador = new PaisAdaptador(ListaPaises, false) { AlTocar = OpcionesDe };
        BotonBuscar.Clicked += (_, _) => Accion(() => controlador!.Buscar());
        CampoFiltro.Entrada.Completed += (_, _) => Accion(() => controlador!.Buscar());
        BotonNuevo.Clicked += (_, _) => Accion(() => controlador!.Registrar());
        PintarTotal(0);
    }

    internal void EstablecerControlador(PaisControlador paisControlador)
    {
        controlador = paisControlador;
    }

    /// <summary>Al tocar un país se elige y se ofrecen Editar y Eliminar.</summary>
    private async void OpcionesDe(Pais pais, View tarjeta)
    {
        seleccionado = pais;
        adaptador.Seleccionar(pais);
        Page? pagina = Principal;
        if (pagina == null)
        {
            return;
        }
        string? opcion = await pagina.DisplayActionSheetAsync(null, null, null, EDITAR, ELIMINAR);
        if (opcion == EDITAR)
        {
            Accion(() => controlador!.Actualizar());
        }
        else if (opcion == ELIMINAR)
        {
            Accion(() => controlador!.Eliminar());
        }
    }

    private void Accion(Func<Task> tarea)
    {
        if (controlador != null)
        {
            errorFormulario = null;
            Ejecutar(tarea);
        }
    }

    private void PintarTotal(int cantidad)
    {
        TotalPaises.Text = Formato.Cantidad(cantidad, "país", "países");
    }

    // ------------------------------------------------------------------ PaisVista

    public override void MostrarError(string? mensaje)
    {
        errorFormulario = mensaje;
        base.MostrarError(mensaje);
    }

    public void MostrarTabla(List<Pais> paises)
    {
        Hilos.EnPrincipal(() =>
        {
            seleccionado = null;
            adaptador.SetPaises(paises);
            adaptador.Seleccionar(null);
            PintarTotal(paises?.Count ?? 0);
        });
    }

    public string? LeerFiltro()
    {
        return Hilos.EnPrincipalYEsperar(() => CampoFiltro.Texto);
    }

    public Pais? LeerPais(Pais? inicial)
    {
        string? error = errorFormulario;
        errorFormulario = null;
        bool nuevo = inicial == null || inicial.Id == 0;
        return EsperarUsuario<Pais>(respuesta => FormularioPais.Mostrar(Principal!, inicial, nuevo, error, respuesta));
    }

    public Pais? PaisSeleccionado() => seleccionado;

    public bool ConfirmarEliminacion(Pais pais)
    {
        bool? confirmado = EsperarUsuario<bool?>(respuesta => Principal!.MostrarDialogo(
            "Eliminar país", "¿Eliminar el país " + pais.Nombre + "?", null, "Eliminar", "Cancelar",
            () =>
            {
                respuesta(true);
                return true;
            },
            () => respuesta(false)));
        return confirmado == true;
    }
}
