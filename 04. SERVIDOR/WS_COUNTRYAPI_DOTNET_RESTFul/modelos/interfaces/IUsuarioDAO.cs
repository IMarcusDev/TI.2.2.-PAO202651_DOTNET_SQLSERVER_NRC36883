using ec.edu.monster.modelos.data;

namespace ec.edu.monster.modelos.interfaces;

/// <summary>
/// Acceso a la tabla Usuarios.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface IUsuarioDAO
{
    /// <summary>Busca un usuario por su username; devuelve null si no existe.</summary>
    Usuario? BuscarPorUsername(string username);
}
