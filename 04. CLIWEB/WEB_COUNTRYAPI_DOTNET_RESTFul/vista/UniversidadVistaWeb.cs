using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Universidades (ECUD10) en universidades.cshtml: importar las de un país (en inglés) y ver
/// las guardadas. Sin país en la dirección se muestran las de Ecuador, como en escritorio.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class UniversidadVistaWeb : VistaWeb, UniversidadVista
{
    public const string UNIVERSIDADES = "universidades";
    public const string PAIS = "pais";
    public const string PAIS_POR_DEFECTO = "Ecuador";

    public string? LeerPais()
    {
        string valor = Parametro(PAIS) ?? PAIS_POR_DEFECTO;
        Poner(PAIS, valor);
        return valor;
    }

    public void MostrarResumen(ResumenImportacion resumen)
    {
        MostrarMensaje("Importación de universidades finalizada: " + resumen.Recibidos + " recibidas, "
            + resumen.Registrados + " registradas y " + resumen.Actualizados + " actualizadas");
    }

    public void MostrarUniversidades(List<Universidad> universidades)
    {
        Poner(UNIVERSIDADES, universidades);
    }
}
