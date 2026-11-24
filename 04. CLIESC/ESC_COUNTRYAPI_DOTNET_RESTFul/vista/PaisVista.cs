using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Tabla de países con búsqueda por nombre y botones Nuevo, Editar y Eliminar (ECUD4).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface PaisVista : VistaBase
{
    void MostrarTabla(List<Pais> paises);

    /// <summary>Texto del campo «Buscar».</summary>
    string? LeerFiltro();

    /// <summary>
    /// Pide los siete datos del país en un formulario.
    /// </summary>
    /// <param name="inicial">datos con los que se abre el formulario (null para uno nuevo)</param>
    /// <returns>el país escrito, o null si el usuario canceló. Si la población o el área no son
    /// números válidos, se devuelven como -1 y NaN para que el validador los rechace.</returns>
    Pais? LeerPais(Pais? inicial);

    /// <summary>País elegido en la tabla, o null si no hay ninguno.</summary>
    Pais? PaisSeleccionado();

    /// <summary>Pide confirmar «¿Eliminar el país &lt;nombre&gt;?».</summary>
    bool ConfirmarEliminacion(Pais pais);
}
