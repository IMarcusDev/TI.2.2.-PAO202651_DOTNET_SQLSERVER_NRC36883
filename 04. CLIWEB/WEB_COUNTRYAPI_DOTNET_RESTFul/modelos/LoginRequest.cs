namespace ec.edu.monster.modelos;

/// <summary>
/// Cuerpo de POST /Login/login.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class LoginRequest
{
    public string? Usuario { get; set; }
    public string? Clave { get; set; }

    public LoginRequest()
    {
    }

    public LoginRequest(string? usuario, string? clave)
    {
        Usuario = usuario;
        Clave = clave;
    }
}
