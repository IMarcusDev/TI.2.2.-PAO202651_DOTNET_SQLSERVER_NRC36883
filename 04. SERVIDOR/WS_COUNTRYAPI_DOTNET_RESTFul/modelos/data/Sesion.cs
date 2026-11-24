namespace ec.edu.monster.modelos.data;

/// <summary>
/// Sesión registrada en memoria por TokenService: el token, su usuario y la hora del último uso
/// (base para la expiración por inactividad de 15 minutos).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class Sesion
{
    public string Token { get; set; } = "";
    public Usuario Usuario { get; set; } = new();
    public DateTime UltimoUso { get; set; }

    public Sesion()
    {
    }

    public Sesion(string token, Usuario usuario, DateTime ultimoUso)
    {
        Token = token;
        Usuario = usuario;
        UltimoUso = ultimoUso;
    }
}
