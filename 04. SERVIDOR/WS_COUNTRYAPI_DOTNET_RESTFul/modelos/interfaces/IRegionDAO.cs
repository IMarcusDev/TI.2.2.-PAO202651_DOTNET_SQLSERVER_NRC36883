using ec.edu.monster.modelos.data;

namespace ec.edu.monster.modelos.interfaces;

/// <summary>
/// Acceso al catálogo Regiones.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface IRegionDAO
{
    /// <summary>Región con ese nombre (sin distinguir mayúsculas ni tildes), o null si no existe.</summary>
    Region? BuscarPorNombre(string nombre);

    /// <summary>Las seis regiones del catálogo.</summary>
    List<Region> Listar();
}
