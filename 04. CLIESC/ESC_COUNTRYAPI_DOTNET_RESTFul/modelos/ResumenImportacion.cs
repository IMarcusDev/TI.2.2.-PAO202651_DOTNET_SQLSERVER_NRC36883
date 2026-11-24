namespace ec.edu.monster.modelos;

/// <summary>
/// Totales de una importación: recibidos de la API, registrados y actualizados.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ResumenImportacion
{
    public int Recibidos { get; set; }
    public int Registrados { get; set; }
    public int Actualizados { get; set; }

    public ResumenImportacion()
    {
    }

    public ResumenImportacion(int recibidos, int registrados, int actualizados)
    {
        Recibidos = recibidos;
        Registrados = registrados;
        Actualizados = actualizados;
    }
}
