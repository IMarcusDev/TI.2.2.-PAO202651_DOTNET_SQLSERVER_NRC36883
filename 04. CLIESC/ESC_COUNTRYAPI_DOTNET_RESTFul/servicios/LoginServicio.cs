using ec.edu.monster.modelos;

namespace ec.edu.monster.servicios;

/// <summary>
/// Inicio y cierre de sesión: /Login/login y /Login/logout.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class LoginServicio
{
    private readonly ClienteRest cliente;

    public LoginServicio(ClienteRest cliente)
    {
        this.cliente = cliente;
    }

    public Task<RespuestaDTO<LoginResponse>> Login(string usuario, string clave)
    {
        return cliente.Enviar<LoginResponse>("POST", "/Login/login", null, new LoginRequest(usuario, clave));
    }

    public Task<RespuestaDTO<object>> Logout(string? token)
    {
        return cliente.Enviar<object>("POST", "/Login/logout", token, null);
    }
}
