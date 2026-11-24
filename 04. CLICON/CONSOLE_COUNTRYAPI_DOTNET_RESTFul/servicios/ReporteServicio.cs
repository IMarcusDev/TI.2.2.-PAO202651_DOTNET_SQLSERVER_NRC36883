using ec.edu.monster.modelos;

namespace ec.edu.monster.servicios;

/// <summary>
/// Reportes estadísticos: GET /Reportes.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ReporteServicio
{
    private readonly ClienteRest cliente;

    public ReporteServicio(ClienteRest cliente)
    {
        this.cliente = cliente;
    }

    public Task<RespuestaDTO<Reporte>> Generar(string? token)
    {
        return cliente.Enviar<Reporte>("GET", "/Reportes", token, null);
    }
}
