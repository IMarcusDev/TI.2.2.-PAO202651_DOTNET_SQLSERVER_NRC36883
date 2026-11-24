using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// «Importar países» (ECUD2): pide confirmación, importa todos los países de REST Countries a
/// través del servidor y muestra cuántos se recibieron, registraron y actualizaron.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public partial class ImportacionPantalla : PantallaBase, ImportacionVista
{
    private ApiControlador? controlador;

    public ImportacionPantalla()
    {
        InitializeComponent();
        Preparar(AvisoPantalla);
        BotonImportar.Clicked += (_, _) =>
        {
            if (controlador != null)
            {
                Ejecutar(controlador.Importar);
            }
        };
    }

    internal void EstablecerControlador(ApiControlador apiControlador)
    {
        controlador = apiControlador;
    }

    // ------------------------------------------------------------------ ImportacionVista

    public bool ConfirmarImportacion()
    {
        bool? confirmado = EsperarUsuario<bool?>(respuesta => Principal!.MostrarDialogo(
            "Importar países",
            "¿Importar los países desde la API REST Countries? Los países existentes se actualizarán",
            null, "Importar", "Cancelar",
            () =>
            {
                respuesta(true);
                return true;
            },
            () => respuesta(false)));
        return confirmado == true;
    }

    public void MostrarResumen(ResumenImportacion resumen)
    {
        Hilos.EnPrincipal(() =>
        {
            Recibidos.Text = Formato.Entero(resumen.Recibidos);
            Registrados.Text = Formato.Entero(resumen.Registrados);
            Actualizados.Text = Formato.Entero(resumen.Actualizados);
            MostrarMensaje("Importación finalizada: " + resumen.Recibidos + " países recibidos, "
                + resumen.Registrados + " registrados y " + resumen.Actualizados + " actualizados");
        });
    }
}
