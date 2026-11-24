using ec.edu.monster.config;
using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.interfaces;
using Microsoft.EntityFrameworkCore;

namespace ec.edu.monster.modelos.repositorios;

/// <summary>
/// DAO del catálogo Regiones. La comparación por nombre no distingue mayúsculas ni tildes
/// («america» es América), igual que en MySQL.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class RegionDAO : IRegionDAO
{
    private readonly AppDbContext contexto;

    public RegionDAO(AppDbContext contexto)
    {
        this.contexto = contexto;
    }

    public Region? BuscarPorNombre(string nombre)
    {
        return contexto.Regiones.AsNoTracking()
            .FirstOrDefault(r => EF.Functions.Collate(r.Nombre, AppDbContext.COLACION_SIN_TILDES) == nombre);
    }

    public List<Region> Listar()
    {
        return contexto.Regiones.AsNoTracking().OrderBy(r => r.IdRegion).ToList();
    }
}
