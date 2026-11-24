using System.Globalization;
using System.Text;
using ec.edu.monster.config;
using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.dto;
using ec.edu.monster.modelos.interfaces;
using Microsoft.EntityFrameworkCore;

namespace ec.edu.monster.modelos.repositorios;

/// <summary>
/// DAO de la tabla Universidades con EF Core (consultas LINQ parametrizadas).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class UniversidadDAO : IUniversidadDAO
{
    private readonly AppDbContext contexto;

    public UniversidadDAO(AppDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <summary>
    /// Importación de las universidades de un país (RQF-010):
    /// <list type="number">
    ///   <item>carga una sola vez las universidades que ya existen de los países recibidos;</item>
    ///   <item>separa cada universidad en nueva (se agrega) o existente (misma pareja nombre y país:
    ///         se actualizan dominio, sitio web y fecha);</item>
    ///   <item>guarda todo con un solo SaveChanges dentro de una sola transacción.</item>
    /// </list>
    /// Si algo falla se hace ROLLBACK.
    /// </summary>
    public async Task<ResumenImportacion> GuardarTodas(List<Universidad> universidades)
    {
        int registradas = 0;
        int actualizadas = 0;
        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        try
        {
            Dictionary<string, Universidad> existentes = await Existentes(universidades);
            var procesadas = new HashSet<string>();
            DateTime ahora = DateTime.Now;
            foreach (Universidad u in universidades)
            {
                string clave = Clave(u.Nombre, u.Pais!);
                if (!procesadas.Add(clave))
                {
                    continue;
                }
                if (existentes.TryGetValue(clave, out Universidad? fila))
                {
                    fila.Dominio = u.Dominio;
                    fila.SitioWeb = u.SitioWeb;
                    fila.FechaActualizacion = ahora;
                    actualizadas++;
                }
                else
                {
                    contexto.Universidades.Add(new Universidad(u.Nombre, u.Dominio, u.SitioWeb, u.Pais)
                    {
                        FechaActualizacion = ahora
                    });
                    registradas++;
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
        return new ResumenImportacion(universidades.Count, registradas, actualizadas);
    }

    /// <summary>Universidades del país (sin distinguir mayúsculas ni tildes), ordenadas por nombre.</summary>
    public List<Universidad> ListarPorPais(string pais)
    {
        string buscado = pais.Trim();
        return contexto.Universidades.AsNoTracking()
            .Where(u => EF.Functions.Collate(u.Pais!, AppDbContext.COLACION_SIN_TILDES) == buscado)
            .OrderBy(u => u.Nombre)
            .ToList();
    }

    /// <summary>Universidades existentes de los países recibidos, por clave nombre|país.</summary>
    private async Task<Dictionary<string, Universidad>> Existentes(List<Universidad> universidades)
    {
        var paises = new HashSet<string>(universidades.Select(u => u.Pais!));
        var filas = new Dictionary<string, Universidad>();
        foreach (string pais in paises)
        {
            List<Universidad> delPais = await contexto.Universidades
                .Where(u => EF.Functions.Collate(u.Pais!, AppDbContext.COLACION_SIN_TILDES) == pais)
                .ToListAsync();
            foreach (Universidad u in delPais)
            {
                filas[Clave(u.Nombre, u.Pais!)] = u;
            }
        }
        return filas;
    }

    /// <summary>Clave nombre|país sin tildes ni mayúsculas, como el índice UNIQUE de MySQL (ai_ci).</summary>
    private static string Clave(string nombre, string pais)
    {
        return Normalizar(nombre) + "|" + Normalizar(pais);
    }

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
