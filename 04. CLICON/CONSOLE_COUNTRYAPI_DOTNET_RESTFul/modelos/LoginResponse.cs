namespace ec.edu.monster.modelos;

/// <summary>
/// Datos de un inicio de sesión correcto: token y nombre del usuario.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class LoginResponse
{
    public string? Token { get; set; }
    public string? Nombre { get; set; }

    public LoginResponse()
    {
    }

    public LoginResponse(string? token, string? nombre)
    {
        Token = token;
        Nombre = nombre;
    }
}
