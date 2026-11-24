using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// «Importar países» (ECUD2) en importar.cshtml. La confirmación es un panel de la página: su
/// botón envía <c>confirmado=si</c>.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ImportacionVistaWeb : VistaWeb, ImportacionVista
{
    public const string RESUMEN = "resumen";

    public bool ConfirmarImportacion()
    {
        return Parametro("confirmado") == "si";
    }

    public void MostrarResumen(ResumenImportacion resumen)
    {
        Poner(RESUMEN, resumen);
        MostrarMensaje("Importación finalizada: " + resumen.Recibidos + " países recibidos, "
            + resumen.Registrados + " registrados y " + resumen.Actualizados + " actualizados");
    }
}
