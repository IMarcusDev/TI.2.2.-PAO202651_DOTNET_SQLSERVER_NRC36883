using System.Security.Cryptography;
using System.Text;
using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.enums;
using ec.edu.monster.modelos.interfaces;
using ec.edu.monster.servicios;

namespace ec.edu.monster.controladores;

/// <summary>
/// Inicio y cierre de sesión (RQF-001, ECUD1).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class LoginControlador
{
    internal const string CREDENCIALES_INCORRECTAS = "Credenciales incorrectas";

    private readonly IUsuarioDAO usuarioDAO;
    private readonly HashService hashService;
    private readonly TokenService tokenService;

    public LoginControlador(IUsuarioDAO usuarioDAO, HashService hashService, TokenService tokenService)
    {
        this.usuarioDAO = usuarioDAO;
        this.hashService = hashService;
        this.tokenService = tokenService;
    }

    /// <summary>
    /// Busca el usuario, compara el hash SHA-256 de la clave con el registrado y verifica que
    /// esté ACTIVO. Si todo es correcto registra un token nuevo.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">«Credenciales incorrectas» (401), sin decir qué dato falló</exception>
    public Sesion Autenticar(string? usuario, string? clave)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(clave))
        {
            throw new UnauthorizedAccessException(CREDENCIALES_INCORRECTAS);
        }
        Usuario? encontrado = usuarioDAO.BuscarPorUsername(usuario.Trim());
        if (encontrado == null
            || encontrado.Estado != EstadoUsuario.ACTIVO
            || !MismoHash(hashService.Sha256(clave), encontrado.PasswordHash))
        {
            throw new UnauthorizedAccessException(CREDENCIALES_INCORRECTAS);
        }
        string token = tokenService.GenerarToken(encontrado);
        return new Sesion(token, encontrado, DateTime.Now);
    }

    /// <summary>Elimina el token de la sesión.</summary>
    public void CerrarSesion(string? token)
    {
        tokenService.EliminarToken(token);
    }

    /// <summary>Compara los dos hash en tiempo constante, sin distinguir mayúsculas.</summary>
    private static bool MismoHash(string calculado, string? registrado)
    {
        if (registrado == null)
        {
            return false;
        }
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(calculado.ToLowerInvariant()),
            Encoding.ASCII.GetBytes(registrado.Trim().ToLowerInvariant()));
    }
}
