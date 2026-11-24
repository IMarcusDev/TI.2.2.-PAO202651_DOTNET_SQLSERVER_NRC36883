namespace ec.edu.monster.modelos.dto;

/// <summary>
/// Totales de una importación (países o universidades): cuántos registros llegaron de la API,
/// cuántos se registraron como nuevos y cuántos se actualizaron.
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
