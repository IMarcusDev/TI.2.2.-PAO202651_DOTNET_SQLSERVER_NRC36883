using ec.edu.monster.controladores;
using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.dto;
using ec.edu.monster.servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ec.edu.monster.ws;

/// <summary>
/// Servicio de autenticación: /api/Login/login y /api/Login/logout (ECUD1).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[Route("api/Login")]
[Produces("application/json")]
public class LoginWebService : ControllerBase
{
    private readonly LoginControlador controlador;
    private readonly TokenService tokenService;
    private readonly ILogger<LoginWebService> log;

    public LoginWebService(LoginControlador controlador, TokenService tokenService, ILogger<LoginWebService> log)
    {
        this.controlador = controlador;
        this.tokenService = tokenService;
        this.log = log;
    }

    /// <summary>POST /api/Login/login con {usuario, clave}: devuelve {token, nombre} o 401.</summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LoginRequestDTO? credenciales)
    {
        try
        {
            Sesion sesion = controlador.Autenticar(credenciales?.Usuario, credenciales?.Clave);
            var datos = new LoginResponseDTO(sesion.Token, sesion.Usuario.Nombre);
            return Ok(RespuestaDTO.Ok("Ingreso de sesión exitoso", datos));
        }
        catch (Exception e)
        {
            return Error(e);
        }
    }

    /// <summary>
    /// POST /api/Login/logout con el token en Authorization. Responde OK aunque el token ya no
    /// exista: el cliente vuelve al inicio de sesión de todas formas (ECUD1, flujo alternativo 4).
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout([FromHeader(Name = "Authorization")] string? authorization)
    {
        tokenService.EliminarToken(authorization);
        return Ok(RespuestaDTO.Ok<object>("Sesión cerrada", null));
    }

    private IActionResult Error(Exception e)
    {
        if (RespuestaDTO.EsErrorInterno(e))
        {
            log.LogError(e, "Error en /Login");
        }
        return StatusCode(RespuestaDTO.CodigoHttp(e), RespuestaDTO.DeExcepcion(e));
    }
}
