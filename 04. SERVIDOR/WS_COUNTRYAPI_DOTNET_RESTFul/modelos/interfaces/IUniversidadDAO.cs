using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.dto;

namespace ec.edu.monster.modelos.interfaces;

/// <summary>
/// Acceso a la tabla Universidades.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface IUniversidadDAO
{
    /// <summary>
    /// Registra las universidades nuevas y actualiza las existentes (misma pareja nombre y país)
    /// en una sola transacción. Si algo falla, deshace todo.
    /// </summary>
    Task<ResumenImportacion> GuardarTodas(List<Universidad> universidades);

    /// <summary>Universidades guardadas de ese país, ordenadas por nombre.</summary>
    List<Universidad> ListarPorPais(string pais);
}
