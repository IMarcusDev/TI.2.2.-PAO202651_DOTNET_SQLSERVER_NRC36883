using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.dto;

namespace ec.edu.monster.modelos.interfaces;

/// <summary>
/// Acceso a la tabla Paises: CRUD, búsquedas, importación en lote y consultas de los reportes.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface IPaisDAO
{
    /// <summary>Todos los países con su región, ordenados por nombre.</summary>
    List<Pais> Listar();

    /// <summary>Países cuyo nombre contiene el texto, sin distinguir mayúsculas ni tildes, ordenados por nombre.</summary>
    List<Pais> BuscarPorNombre(string texto);

    /// <summary>
    /// Países que cumplen todos los criterios indicados (los null se ignoran): nombre y capital
    /// por coincidencia parcial, región por igualdad. Ordenados por nombre.
    /// </summary>
    List<Pais> Buscar(string? nombre, string? capital, int? idRegion);

    /// <summary>País con ese id, o null si no existe.</summary>
    Pais? BuscarPorId(int id);

    /// <summary>True si ya existe un país con ese nombre (sin distinguir mayúsculas ni tildes).</summary>
    bool ExistePorNombre(string nombre);

    /// <summary>Inserta el país y devuelve el id generado.</summary>
    int Insertar(Pais pais);

    /// <summary>Actualiza el país por su id; devuelve false si no existe.</summary>
    bool Actualizar(Pais pais);

    /// <summary>Elimina el país; devuelve false si no existe.</summary>
    bool Eliminar(int id);

    /// <summary>
    /// Registra los países nuevos y actualiza los existentes (comparando por nombre) en una sola
    /// transacción. Si algo falla, deshace todo.
    /// </summary>
    Task<ResumenImportacion> GuardarTodos(List<Pais> paises);

    /// <summary>Registra el país si no existe un país con ese nombre, o lo actualiza si existe.</summary>
    Pais GuardarOActualizar(Pais pais);

    /// <summary>Cantidad de países de cada región, incluidas las regiones sin países, de mayor a menor.</summary>
    List<ConteoRegion> ContarPorRegion();

    /// <summary>País con la mayor población (el primero por nombre si hay empate), o null si no hay países.</summary>
    Pais? PaisMasPoblado();

    /// <summary>Promedio de población de cada región, redondeado a entero; 0 para las regiones sin países.</summary>
    List<PromedioRegion> PromedioPoblacionPorRegion();
}
