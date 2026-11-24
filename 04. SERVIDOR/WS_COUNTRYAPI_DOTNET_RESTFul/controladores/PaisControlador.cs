using System.Globalization;
using System.Text;
using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.dto;
using ec.edu.monster.modelos.interfaces;
using ec.edu.monster.modelos.repositorios;
using ec.edu.monster.servicios;

namespace ec.edu.monster.controladores;

/// <summary>
/// Reglas de negocio de los países: CRUD (RQF-004), búsqueda y filtros (RQF-005),
/// importación (RQF-002) y consulta en la API (RQF-003).
///
/// Los errores se informan con excepciones estándar que RespuestaDTO convierte en el código
/// HTTP: ArgumentException (400), KeyNotFoundException (404), InvalidOperationException (409),
/// IOException de la API (502) y errores de la base (500).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class PaisControlador
{
    internal const string NOMBRE_INVALIDO = "Ingrese el nombre del país";
    internal const string SELECCIONE_REGION = "Seleccione una región";
    internal const string REGION_NO_VALIDA = "Región no válida";
    internal const string POBLACION_INVALIDA = "La población debe ser un número entero mayor o igual que cero";
    internal const string AREA_INVALIDA = "El área debe ser un número mayor o igual que cero";
    internal const string TEXTOS_LARGOS = "La capital, la subregión y la moneda deben tener como máximo 100 caracteres";
    internal const string NOMBRE_REPETIDO = "Ya existe un país con ese nombre";
    internal const string PAIS_NO_EXISTE = "El país no existe";
    internal const string NOMBRE_CORTO = "Ingrese al menos dos caracteres";

    private const int LARGO_MAXIMO = 100;

    private readonly IPaisDAO paisDAO;
    private readonly IRegionDAO regionDAO;
    private readonly ConsumoAPI consumoAPI;
    private readonly ILogger<PaisControlador>? log;

    public PaisControlador(IPaisDAO paisDAO, IRegionDAO regionDAO, ConsumoAPI consumoAPI,
        ILogger<PaisControlador>? log = null)
    {
        this.paisDAO = paisDAO;
        this.regionDAO = regionDAO;
        this.consumoAPI = consumoAPI;
        this.log = log;
    }

    /// <summary>Países almacenados, ordenados por nombre.</summary>
    public List<Pais> ListarPaises()
    {
        return paisDAO.Listar();
    }

    /// <summary>Países cuyo nombre contiene el texto; sin texto, el listado completo.</summary>
    public List<Pais> BuscarPorNombre(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return paisDAO.Listar();
        }
        return paisDAO.BuscarPorNombre(texto.Trim());
    }

    /// <summary>Registra un país nuevo (HTTP 201). El nombre no puede repetirse.</summary>
    public Pais RegistrarPais(Pais? pais)
    {
        Validar(pais);
        if (paisDAO.ExistePorNombre(pais!.Nombre))
        {
            throw new InvalidOperationException(NOMBRE_REPETIDO);
        }
        try
        {
            pais.Id = paisDAO.Insertar(pais);
        }
        catch (Exception e) when (PaisDAO.EsNombreRepetido(e))
        {
            // Otro usuario registró el mismo nombre entre la verificación y el INSERT.
            throw new InvalidOperationException(NOMBRE_REPETIDO);
        }
        return pais;
    }

    /// <summary>
    /// Actualiza los siete datos del país.
    ///
    /// En Java el índice UNIQUE de MySQL detecta los nombres repetidos sin distinguir tildes; el de
    /// SQL Server sí las distingue, así que aquí se comprueba antes de actualizar (DUDAS.md, H4):
    /// si el país no existe, 404; si el nombre cambió y ya lo usa otro país, 409.
    /// </summary>
    public Pais ActualizarPais(int id, Pais? pais)
    {
        Validar(pais);
        pais!.Id = id;
        Pais? actual = paisDAO.BuscarPorId(id);
        if (actual == null)
        {
            throw new KeyNotFoundException(PAIS_NO_EXISTE);
        }
        if (Normalizar(actual.Nombre) != Normalizar(pais.Nombre) && paisDAO.ExistePorNombre(pais.Nombre))
        {
            throw new InvalidOperationException(NOMBRE_REPETIDO);
        }
        bool actualizado;
        try
        {
            actualizado = paisDAO.Actualizar(pais);
        }
        catch (Exception e) when (PaisDAO.EsNombreRepetido(e))
        {
            throw new InvalidOperationException(NOMBRE_REPETIDO);
        }
        if (!actualizado)
        {
            throw new KeyNotFoundException(PAIS_NO_EXISTE);
        }
        return paisDAO.BuscarPorId(id) ?? pais;
    }

    /// <summary>Elimina el país; 404 si no existe.</summary>
    public void EliminarPais(int id)
    {
        if (!paisDAO.Eliminar(id))
        {
            throw new KeyNotFoundException(PAIS_NO_EXISTE);
        }
    }

    /// <summary>
    /// Búsqueda combinable: nombre y capital por coincidencia parcial sin distinguir mayúsculas
    /// ni tildes, región por igualdad. Sin criterios devuelve todos los países.
    /// </summary>
    public List<Pais> BuscarPaises(string? nombre, string? capital, string? region)
    {
        int? idRegion = null;
        if (!string.IsNullOrWhiteSpace(region))
        {
            Region? encontrada = regionDAO.BuscarPorNombre(region.Trim());
            if (encontrada == null)
            {
                throw new ArgumentException(REGION_NO_VALIDA);
            }
            idRegion = encontrada.IdRegion;
        }
        return paisDAO.Buscar(VacioANulo(nombre), VacioANulo(capital), idRegion);
    }

    /// <summary>
    /// Importa todos los países de REST Countries (ECUD2). Si la API falla, se lanza la IOException
    /// antes de tocar la base. Los países se guardan en una sola transacción.
    /// </summary>
    public async Task<ResumenImportacion> ImportarPaises()
    {
        List<Pais> recibidos = await consumoAPI.ObtenerTodos();

        // Cada región traducida por ConsumoAPI se reemplaza por la del catálogo, con su id.
        var regiones = new Dictionary<string, Region>();
        foreach (Region r in regionDAO.Listar())
        {
            regiones[r.Nombre] = r;
        }
        var paraGuardar = new List<Pais>(recibidos.Count);
        foreach (Pais pais in recibidos)
        {
            if (pais.NombreRegion == null || !regiones.TryGetValue(pais.NombreRegion, out Region? region))
            {
                log?.LogWarning("País descartado, región fuera del catálogo: {Nombre}", pais.Nombre);
                continue;
            }
            pais.AsignarRegion(region);
            paraGuardar.Add(pais);
        }

        ResumenImportacion resumen = await paisDAO.GuardarTodos(paraGuardar);
        resumen.Recibidos = recibidos.Count;
        return resumen;
    }

    /// <summary>
    /// Consulta los países que coinciden con el nombre en la API (ECUD3) y marca los que ya
    /// están guardados.
    /// </summary>
    public async Task<List<PaisDTO>> ConsultarEnAPI(string? nombre)
    {
        if (nombre == null || nombre.Trim().Length < 2)
        {
            throw new ArgumentException(NOMBRE_CORTO);
        }
        var resultado = new List<PaisDTO>();
        foreach (Pais pais in await consumoAPI.BuscarPorNombre(nombre))
        {
            resultado.Add(PaisDTO.Desde(pais, paisDAO.ExistePorNombre(pais.Nombre)));
        }
        return resultado;
    }

    /// <summary>Guarda el país elegido de la consulta a la API: lo registra o lo actualiza por nombre.</summary>
    public Pais GuardarDesdeAPI(Pais? pais)
    {
        Validar(pais);
        return paisDAO.GuardarOActualizar(pais!);
    }

    /// <summary>
    /// Repite en el servidor las validaciones del cliente (ECUD4, flujo alternativo 2), limpia
    /// los espacios y completa la región con su id del catálogo.
    /// </summary>
    private void Validar(Pais? pais)
    {
        if (pais == null || string.IsNullOrWhiteSpace(pais.Nombre) || pais.Nombre.Trim().Length > LARGO_MAXIMO)
        {
            throw new ArgumentException(NOMBRE_INVALIDO);
        }
        string? nombreRegion = pais.NombreRegion;
        if (string.IsNullOrWhiteSpace(nombreRegion))
        {
            throw new ArgumentException(SELECCIONE_REGION);
        }
        if (pais.Poblacion < 0)
        {
            throw new ArgumentException(POBLACION_INVALIDA);
        }
        if (pais.Area < 0 || double.IsNaN(pais.Area) || double.IsInfinity(pais.Area))
        {
            throw new ArgumentException(AREA_INVALIDA);
        }
        pais.Nombre = pais.Nombre.Trim();
        pais.Capital = VacioANulo(pais.Capital);
        pais.SubRegion = VacioANulo(pais.SubRegion);
        pais.Moneda = VacioANulo(pais.Moneda);
        if (MuyLargo(pais.Capital) || MuyLargo(pais.SubRegion) || MuyLargo(pais.Moneda))
        {
            throw new ArgumentException(TEXTOS_LARGOS);
        }
        Region? region = regionDAO.BuscarPorNombre(nombreRegion.Trim());
        if (region == null)
        {
            throw new ArgumentException(REGION_NO_VALIDA);
        }
        pais.AsignarRegion(region);
    }

    private static bool MuyLargo(string? texto)
    {
        return texto != null && texto.Length > LARGO_MAXIMO;
    }

    private static string? VacioANulo(string? texto)
    {
        return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }

    /// <summary>Clave de comparación sin tildes ni mayúsculas, como la intercalación ai_ci de MySQL.</summary>
    private static string Normalizar(string texto)
    {
        string descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
        var sinTildes = new StringBuilder(descompuesto.Length);
        foreach (char c in descompuesto)
        {
            UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria != UnicodeCategory.NonSpacingMark
                && categoria != UnicodeCategory.SpacingCombiningMark
                && categoria != UnicodeCategory.EnclosingMark)
            {
                sinTildes.Append(c);
            }
        }
        return sinTildes.ToString().ToLowerInvariant();
    }
}
