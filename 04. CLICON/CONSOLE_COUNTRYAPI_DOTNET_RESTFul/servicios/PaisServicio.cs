using ec.edu.monster.modelos;

namespace ec.edu.monster.servicios;

/// <summary>
/// Operaciones de países: CRUD, búsqueda, importación y consulta en la API (/Paises).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class PaisServicio
{
    private readonly ClienteRest cliente;

    public PaisServicio(ClienteRest cliente)
    {
        this.cliente = cliente;
    }

    /// <summary>Países guardados; con nombre, solo los que lo contienen.</summary>
    public Task<RespuestaDTO<List<Pais>>> Listar(string? token, string? nombre)
    {
        string ruta = string.IsNullOrWhiteSpace(nombre) ? "/Paises" : "/Paises?nombre=" + ClienteRest.Codificar(nombre.Trim());
        return cliente.Enviar<List<Pais>>("GET", ruta, token, null);
    }

    public Task<RespuestaDTO<Pais>> Registrar(string? token, Pais pais)
    {
        return cliente.Enviar<Pais>("POST", "/Paises", token, pais);
    }

    public Task<RespuestaDTO<Pais>> Actualizar(string? token, int id, Pais pais)
    {
        return cliente.Enviar<Pais>("PUT", "/Paises/" + id, token, pais);
    }

    public Task<RespuestaDTO<object>> Eliminar(string? token, int id)
    {
        return cliente.Enviar<object>("DELETE", "/Paises/" + id, token, null);
    }

    /// <summary>GET /Paises/buscar con los criterios que tienen valor.</summary>
    public Task<RespuestaDTO<List<Pais>>> Buscar(string? token, CriteriosBusqueda criterios)
    {
        var query = new List<string>();
        Agregar(query, "nombre", criterios.Nombre);
        Agregar(query, "capital", criterios.Capital);
        Agregar(query, "region", criterios.Region);
        string ruta = "/Paises/buscar" + (query.Count == 0 ? "" : "?" + string.Join("&", query));
        return cliente.Enviar<List<Pais>>("GET", ruta, token, null);
    }

    public Task<RespuestaDTO<ResumenImportacion>> Importar(string? token)
    {
        return cliente.Enviar<ResumenImportacion>("POST", "/Paises/importar", token, null);
    }

    public Task<RespuestaDTO<List<Pais>>> ConsultarExterno(string? token, string nombre)
    {
        return cliente.Enviar<List<Pais>>("GET", "/Paises/externo/" + ClienteRest.Codificar(nombre.Trim()), token, null);
    }

    public Task<RespuestaDTO<Pais>> Guardar(string? token, Pais pais)
    {
        return cliente.Enviar<Pais>("POST", "/Paises/guardar", token, pais);
    }

    private static void Agregar(List<string> query, string clave, string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor))
        {
            query.Add(clave + "=" + ClienteRest.Codificar(valor.Trim()));
        }
    }
}
