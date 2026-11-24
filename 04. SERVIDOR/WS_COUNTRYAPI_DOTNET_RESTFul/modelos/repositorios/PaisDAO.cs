using System.Globalization;
using System.Text;
using ec.edu.monster.config;
using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.dto;
using ec.edu.monster.modelos.interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ec.edu.monster.modelos.repositorios;

/// <summary>
/// DAO de la tabla Paises con EF Core. Las consultas LINQ se envían a SQL Server como sentencias
/// parametrizadas, así que no hay riesgo de inyección SQL.
///
/// Las búsquedas e igualdades por nombre usan la intercalación Latin1_General_CI_AI, que no distingue
/// mayúsculas ni tildes («peru» encuentra «Perú»), igual que utf8mb4_0900_ai_ci en el MySQL de TI.2.1.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class PaisDAO : IPaisDAO
{
    private readonly AppDbContext contexto;

    public PaisDAO(AppDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <summary>Países con su región, sin seguimiento de cambios (solo lectura).</summary>
    private IQueryable<Pais> ConRegion() => contexto.Paises.AsNoTracking().Include(p => p.Region);

    public List<Pais> Listar()
    {
        return ConRegion().OrderBy(p => p.Nombre).ToList();
    }

    public List<Pais> BuscarPorNombre(string texto)
    {
        string patron = Contiene(texto);
        return ConRegion()
            .Where(p => EF.Functions.Like(EF.Functions.Collate(p.Nombre, AppDbContext.COLACION_SIN_TILDES), patron, "\\"))
            .OrderBy(p => p.Nombre)
            .ToList();
    }

    public List<Pais> Buscar(string? nombre, string? capital, int? idRegion)
    {
        // Solo se agregan al WHERE los criterios que tienen valor; los valores van como parámetros.
        IQueryable<Pais> consulta = ConRegion();
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            string patron = Contiene(nombre);
            consulta = consulta.Where(p =>
                EF.Functions.Like(EF.Functions.Collate(p.Nombre, AppDbContext.COLACION_SIN_TILDES), patron, "\\"));
        }
        if (!string.IsNullOrWhiteSpace(capital))
        {
            string patron = Contiene(capital);
            consulta = consulta.Where(p =>
                EF.Functions.Like(EF.Functions.Collate(p.Capital!, AppDbContext.COLACION_SIN_TILDES), patron, "\\"));
        }
        if (idRegion != null)
        {
            consulta = consulta.Where(p => p.IdRegion == idRegion.Value);
        }
        return consulta.OrderBy(p => p.Nombre).ToList();
    }

    public Pais? BuscarPorId(int id)
    {
        return ConRegion().FirstOrDefault(p => p.Id == id);
    }

    public bool ExistePorNombre(string nombre)
    {
        return BuscarIdPorNombre(nombre) != null;
    }

    public int Insertar(Pais pais)
    {
        Pais nuevo = NuevaFila(pais, DateTime.Now);
        contexto.Paises.Add(nuevo);
        contexto.SaveChanges();
        return nuevo.Id;
    }

    /// <summary>Actualización completa desde el CRUD, incluido el nombre (un solo UPDATE por id).</summary>
    public bool Actualizar(Pais pais)
    {
        DateTime ahora = DateTime.Now;
        int filas = contexto.Paises
            .Where(p => p.Id == pais.Id)
            .ExecuteUpdate(s => s
                .SetProperty(p => p.Nombre, pais.Nombre)
                .SetProperty(p => p.Capital, pais.Capital)
                .SetProperty(p => p.IdRegion, pais.IdRegion)
                .SetProperty(p => p.SubRegion, pais.SubRegion)
                .SetProperty(p => p.Poblacion, pais.Poblacion)
                .SetProperty(p => p.Area, pais.Area)
                .SetProperty(p => p.Moneda, pais.Moneda)
                .SetProperty(p => p.FechaActualizacion, ahora));
        return filas > 0;
    }

    public bool Eliminar(int id)
    {
        return contexto.Paises.Where(p => p.Id == id).ExecuteDelete() > 0;
    }

    /// <summary>
    /// Importación masiva (RQF-002, RNF-006):
    /// <list type="number">
    ///   <item>carga una sola vez los países que ya existen;</item>
    ///   <item>separa cada país recibido en nuevo (se agrega) o existente (se actualizan sus datos);</item>
    ///   <item>guarda todo con un solo SaveChanges dentro de una sola transacción.</item>
    /// </list>
    /// Si cualquier sentencia falla se hace ROLLBACK y la base queda como estaba.
    /// Los nombres se comparan sin mayúsculas ni tildes, y si la API trae dos países con el mismo
    /// nombre solo se guarda el primero.
    /// </summary>
    public async Task<ResumenImportacion> GuardarTodos(List<Pais> paises)
    {
        int registrados = 0;
        int actualizados = 0;
        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            var existentes = new Dictionary<string, Pais>();
            foreach (Pais existente in await contexto.Paises.ToListAsync())
            {
                existentes[Normalizar(existente.Nombre)] = existente;
            }
            var procesados = new HashSet<string>();
            DateTime ahora = DateTime.Now;
            foreach (Pais pais in paises)
            {
                string clave = Normalizar(pais.Nombre);
                if (!procesados.Add(clave))
                {
                    continue;
                }
                if (existentes.TryGetValue(clave, out Pais? fila))
                {
                    CopiarDatosApi(pais, fila, ahora);
                    actualizados++;
                }
                else
                {
                    contexto.Paises.Add(NuevaFila(pais, ahora));
                    registrados++;
                }
            }
            await contexto.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            contexto.ChangeTracker.Clear();
            throw;
        }
        return new ResumenImportacion(paises.Count, registrados, actualizados);
    }

    /// <summary>Guardado de un país elegido en la consulta a la API: registra o actualiza por nombre.</summary>
    public Pais GuardarOActualizar(Pais pais)
    {
        using var transaccion = contexto.Database.BeginTransaction();
        try
        {
            DateTime ahora = DateTime.Now;
            Pais? fila = contexto.Paises
                .FirstOrDefault(p => EF.Functions.Collate(p.Nombre, AppDbContext.COLACION_SIN_TILDES) == pais.Nombre);
            if (fila == null)
            {
                fila = NuevaFila(pais, ahora);
                contexto.Paises.Add(fila);
            }
            else
            {
                CopiarDatosApi(pais, fila, ahora);
            }
            contexto.SaveChanges();
            transaccion.Commit();
            pais.Id = fila.Id;
            return pais;
        }
        catch
        {
            transaccion.Rollback();
            contexto.ChangeTracker.Clear();
            throw;
        }
    }

    public List<ConteoRegion> ContarPorRegion()
    {
        // Se parte de Regiones para que también aparezcan las regiones sin países (con 0).
        return contexto.Regiones.AsNoTracking()
            .Select(r => new
            {
                r.Nombre,
                Cantidad = contexto.Paises.Count(p => p.IdRegion == r.IdRegion)
            })
            .OrderByDescending(x => x.Cantidad).ThenBy(x => x.Nombre)
            .AsEnumerable()
            .Select(x => new ConteoRegion(x.Nombre, x.Cantidad))
            .ToList();
    }

    public Pais? PaisMasPoblado()
    {
        return ConRegion().OrderByDescending(p => p.Poblacion).ThenBy(p => p.Nombre).FirstOrDefault();
    }

    /// <summary>
    /// En SQL Server, AVG sobre BIGINT devuelve un entero truncado; en MySQL, un decimal que luego se
    /// redondea. Por eso se traen la suma y la cantidad de cada región y el promedio se calcula aquí
    /// como lo hace MySQL (DUDAS.md, H7). Las regiones sin países quedan en 0.
    /// </summary>
    public List<PromedioRegion> PromedioPoblacionPorRegion()
    {
        var filas = contexto.Regiones.AsNoTracking()
            .OrderBy(r => r.Nombre)
            .Select(r => new
            {
                r.Nombre,
                Suma = contexto.Paises.Where(p => p.IdRegion == r.IdRegion).Sum(p => (long?)p.Poblacion) ?? 0L,
                Cantidad = contexto.Paises.Count(p => p.IdRegion == r.IdRegion)
            })
            .ToList();
        // OrderByDescending es estable: los empates conservan el orden por nombre de SQL Server.
        return filas
            .Select(f => new PromedioRegion(f.Nombre, Promedio(f.Suma, f.Cantidad)))
            .OrderByDescending(p => p.PromedioPoblacion)
            .ToList();
    }

    /// <summary>
    /// True si la excepción es la violación de un índice único (2601 o 2627 en SQL Server), el
    /// equivalente de SQLIntegrityConstraintViolationException en Java.
    /// </summary>
    public static bool EsNombreRepetido(Exception e)
    {
        for (Exception? actual = e; actual != null; actual = actual.InnerException)
        {
            if (actual is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            {
                return true;
            }
        }
        return false;
    }

    // ------------------------------------------------------------------ auxiliares

    private int? BuscarIdPorNombre(string nombre)
    {
        return contexto.Paises.AsNoTracking()
            .Where(p => EF.Functions.Collate(p.Nombre, AppDbContext.COLACION_SIN_TILDES) == nombre)
            .Select(p => (int?)p.Id)
            .FirstOrDefault();
    }

    /// <summary>Fila nueva con los siete datos; la región va solo por su id.</summary>
    private static Pais NuevaFila(Pais pais, DateTime ahora)
    {
        return new Pais
        {
            Nombre = pais.Nombre,
            Capital = pais.Capital,
            IdRegion = pais.IdRegion,
            SubRegion = pais.SubRegion,
            Poblacion = pais.Poblacion,
            Area = pais.Area,
            Moneda = pais.Moneda,
            FechaActualizacion = ahora
        };
    }

    /// <summary>Actualización desde la API: el país se identifica por nombre, que no cambia (ECUD2 paso 7).</summary>
    private static void CopiarDatosApi(Pais origen, Pais fila, DateTime ahora)
    {
        fila.Capital = origen.Capital;
        fila.IdRegion = origen.IdRegion;
        fila.SubRegion = origen.SubRegion;
        fila.Poblacion = origen.Poblacion;
        fila.Area = origen.Area;
        fila.Moneda = origen.Moneda;
        fila.FechaActualizacion = ahora;
    }

    private static long Promedio(long suma, int cantidad)
    {
        if (cantidad == 0)
        {
            return 0;
        }
        // MySQL calcula AVG con 4 decimales y ROUND lo lleva al entero; ambos redondean alejándose de cero.
        decimal conCuatroDecimales = Math.Round((decimal)suma / cantidad, 4, MidpointRounding.AwayFromZero);
        return (long)Math.Round(conCuatroDecimales, 0, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Patrón LIKE «contiene»: escapa con «\» los comodines que escriba el usuario. Además de % y _
    /// (como en Java), escapa [ porque en SQL Server también es comodín (DUDAS.md, H5).
    /// </summary>
    private static string Contiene(string texto)
    {
        string escapado = texto.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_")
            .Replace("[", "\\[");
        return "%" + escapado + "%";
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
