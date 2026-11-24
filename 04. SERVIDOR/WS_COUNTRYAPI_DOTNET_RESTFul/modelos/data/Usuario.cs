using ec.edu.monster.modelos.enums;

namespace ec.edu.monster.modelos.data;

/// <summary>
/// Fila de la tabla Usuarios. La clave se guarda como hash SHA-256 (PasswordHash).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Usuario
{
    public int IdUsuario { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Nombre { get; set; } = "";
    public EstadoUsuario Estado { get; set; }

    public Usuario()
    {
    }

    public Usuario(int idUsuario, string username, string passwordHash, string nombre, EstadoUsuario estado)
    {
        IdUsuario = idUsuario;
        Username = username;
        PasswordHash = passwordHash;
        Nombre = nombre;
        Estado = estado;
    }
}
