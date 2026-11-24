using ec.edu.monster.modelos;

namespace ec.edu.monster.servicios;

/// <summary>
/// Universidades: importar desde la API (vía el servidor) y listar las guardadas.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class UniversidadServicio
{
    private readonly ClienteRest cliente;

    public UniversidadServicio(ClienteRest cliente)
    {
        this.cliente = cliente;
    }

    public Task<RespuestaDTO<ResumenImportacion>> Importar(string? token, string pais)
    {
        return cliente.Enviar<ResumenImportacion>("POST",
            "/Universidades/importar?pais=" + ClienteRest.Codificar(pais.Trim()), token, null);
    }

    public Task<RespuestaDTO<List<Universidad>>> Listar(string? token, string pais)
    {
        return cliente.Enviar<List<Universidad>>("GET", "/Universidades?pais=" + ClienteRest.Codificar(pais.Trim()), token, null);
    }
}
