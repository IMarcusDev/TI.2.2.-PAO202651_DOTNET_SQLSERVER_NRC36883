namespace ec.edu.monster.modelos.dto;

/// <summary>
/// Datos de un inicio de sesión correcto: el token y el nombre del usuario.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class LoginResponseDTO
{
    public string Token { get; set; } = "";
    public string Nombre { get; set; } = "";

    public LoginResponseDTO()
    {
    }

    public LoginResponseDTO(string token, string nombre)
    {
        Token = token;
        Nombre = nombre;
    }
}
