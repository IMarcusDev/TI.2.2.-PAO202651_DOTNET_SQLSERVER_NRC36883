using ec.edu.monster.controladores;
using ec.edu.monster.modelos.dto;
using ec.edu.monster.servicios;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.ws;

/// <summary>
/// Servicio de universidades: /api/Universidades (ECUD10).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[Route("api/Universidades")]
[Produces("application/json")]
public class UniversidadWebService : ControllerBase
{
    private readonly UniversidadControlador controlador;
    private readonly TokenService tokenService;
    private readonly ILogger<UniversidadWebService> log;

    public UniversidadWebService(UniversidadControlador controlador, TokenService tokenService,
        ILogger<UniversidadWebService> log)
    {
        this.controlador = controlador;
        this.tokenService = tokenService;
        this.log = log;
    }

    /// <summary>POST /api/Universidades/importar?pais=Ecuador: importa las universidades del país.</summary>
    [HttpPost("importar")]
    public async Task<IActionResult> Importar([FromHeader(Name = "Authorization")] string? authorization,
        [FromQuery] string? pais)
    {
        if (tokenService.ValidarToken(authorization) == null)
        {
            return TokenInvalido();
        }
        try
        {
            return Ok(RespuestaDTO.Ok("Importación de universidades finalizada",
                await controlador.ImportarUniversidades(pais)));
        }
        catch (Exception e)
        {
            return Error(e);
        }
    }

    /// <summary>GET /api/Universidades?pais=Ecuador: universidades guardadas del país.</summary>
    [HttpGet]
    public IActionResult Listar([FromHeader(Name = "Authorization")] string? authorization, [FromQuery] string? pais)
    {
        if (tokenService.ValidarToken(authorization) == null)
        {
            return TokenInvalido();
        }
        try
        {
            return Ok(RespuestaDTO.Ok("Consulta realizada correctamente", controlador.ListarUniversidades(pais)));
        }
        catch (Exception e)
        {
            return Error(e);
        }
    }

    private ObjectResult TokenInvalido()
    {
        return StatusCode(401, RespuestaDTO.Error(RespuestaDTO.TOKEN_INVALIDO));
    }

    private ObjectResult Error(Exception e)
    {
        if (RespuestaDTO.EsErrorInterno(e))
        {
            log.LogError(e, "Error en /Universidades");
        }
        return StatusCode(RespuestaDTO.CodigoHttp(e), RespuestaDTO.DeExcepcion(e));
    }
}
