using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.dto;
using ec.edu.monster.modelos.interfaces;
using Microsoft.EntityFrameworkCore;

namespace ec.edu.monster.controladores;

/// <summary>
/// DAO en memoria para probar los controladores sin SQL Server. Imitan las restricciones de la
/// base: nombre de país único y catálogo de seis regiones.
/// </summary>
internal static class DaosFalsos
{
    internal sealed class RegionDAOFalso : IRegionDAO
    {
        private readonly List<Region> regiones =
        [
            new(1, "América"), new(2, "Europa"), new(3, "Asia"),
            new(4, "África"), new(5, "Oceanía"), new(6, "Antártida")
        ];

        public Region? BuscarPorNombre(string nombre)
        {
            return regiones.FirstOrDefault(r => string.Equals(r.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
        }

        public List<Region> Listar()
        {
            return regiones;
        }
    }

    internal sealed class PaisDAOFalso : IPaisDAO
    {
        internal readonly Dictionary<int, Pais> paises = new();
        private int siguienteId = 1;
        internal List<Pais>? ultimoLoteImportado;

        internal Pais Agregar(string nombre, string region, long poblacion)
        {
            var p = new Pais(nombre, "Capital", new Region(0, region), "Sub", poblacion, 10, "Moneda")
            {
                Id = siguienteId++
            };
            paises[p.Id] = p;
            return p;
        }

        public List<Pais> Listar()
        {
            return paises.Values.OrderBy(p => p.Nombre, StringComparer.Ordinal).ToList();
        }

        public List<Pais> BuscarPorNombre(string texto)
        {
            return Listar().Where(p => p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<Pais> Buscar(string? nombre, string? capital, int? idRegion)
        {
            return Listar()
                .Where(p => nombre == null || p.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase))
                .Where(p => idRegion == null || p.IdRegion == idRegion)
                .ToList();
        }

        public Pais? BuscarPorId(int id)
        {
            return paises.GetValueOrDefault(id);
        }

        public bool ExistePorNombre(string nombre)
        {
            return paises.Values.Any(p => string.Equals(p.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
        }

        public int Insertar(Pais pais)
        {
            if (ExistePorNombre(pais.Nombre))
            {
                throw new DbUpdateException("Duplicate entry");
            }
            pais.Id = siguienteId++;
            paises[pais.Id] = pais;
            return pais.Id;
        }

        public bool Actualizar(Pais pais)
        {
            if (!paises.ContainsKey(pais.Id))
            {
                return false;
            }
            bool repetido = paises.Values.Any(p => p.Id != pais.Id
                && string.Equals(p.Nombre, pais.Nombre, StringComparison.OrdinalIgnoreCase));
            if (repetido)
            {
                throw new DbUpdateException("Duplicate entry");
            }
            paises[pais.Id] = pais;
            return true;
        }

        public bool Eliminar(int id)
        {
            return paises.Remove(id);
        }

        public Task<ResumenImportacion> GuardarTodos(List<Pais> lote)
        {
            ultimoLoteImportado = new List<Pais>(lote);
            int registrados = 0;
            int actualizados = 0;
            foreach (Pais p in lote)
            {
                if (ExistePorNombre(p.Nombre))
                {
                    actualizados++;
                }
                else
                {
                    p.Id = siguienteId++;
                    paises[p.Id] = p;
                    registrados++;
                }
            }
            return Task.FromResult(new ResumenImportacion(lote.Count, registrados, actualizados));
        }

        public Pais GuardarOActualizar(Pais pais)
        {
            pais.Id = siguienteId++;
            paises[pais.Id] = pais;
            return pais;
        }

        public List<ConteoRegion> ContarPorRegion()
        {
            var conteo = new Dictionary<string, int>();
            foreach (Region r in new RegionDAOFalso().Listar())
            {
                conteo[r.Nombre] = 0;
            }
            foreach (Pais p in paises.Values)
            {
                conteo[p.NombreRegion!] = conteo.GetValueOrDefault(p.NombreRegion!) + 1;
            }
            return conteo.Select(par => new ConteoRegion(par.Key, par.Value)).ToList();
        }

        public Pais? PaisMasPoblado()
        {
            return paises.Values.MaxBy(p => p.Poblacion);
        }

        public List<PromedioRegion> PromedioPoblacionPorRegion()
        {
            return [];
        }
    }
}
