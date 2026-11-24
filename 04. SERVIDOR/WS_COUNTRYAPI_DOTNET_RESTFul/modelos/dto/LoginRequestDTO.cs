namespace ec.edu.monster.modelos.dto;

/// <summary>
/// Cuerpo de POST /api/Login/login: {"usuario": "...", "clave": "..."}.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class LoginRequestDTO
{
    public string? Usuario { get; set; }
    public string? Clave { get; set; }

    public LoginRequestDTO()
    {
    }

    public LoginRequestDTO(string? usuario, string? clave)
    {
        Usuario = usuario;
        Clave = clave;
    }
}
