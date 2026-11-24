using ec.edu.monster.controladores;
using ec.edu.monster.modelos.dto;
using ec.edu.monster.servicios;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.ws;

/// <summary>
/// Servicio de reportes: GET /api/Reportes (ECUD6).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[Route("api/Reportes")]
[Produces("application/json")]
public class ReporteWebService : ControllerBase
{
    private readonly ReporteControlador controlador;
    private readonly TokenService tokenService;
    private readonly ILogger<ReporteWebService> log;

    public ReporteWebService(ReporteControlador controlador, TokenService tokenService, ILogger<ReporteWebService> log)
    {
        this.controlador = controlador;
        this.tokenService = tokenService;
        this.log = log;
    }

    /// <summary>Los tres reportes en una sola respuesta, o 404 si no hay países.</summary>
    [HttpGet]
    public IActionResult Generar([FromHeader(Name = "Authorization")] string? authorization)
    {
        if (tokenService.ValidarToken(authorization) == null)
        {
            return StatusCode(401, RespuestaDTO.Error(RespuestaDTO.TOKEN_INVALIDO));
        }
        try
        {
            return Ok(RespuestaDTO.Ok("Reportes generados correctamente", controlador.GenerarReportes()));
        }
        catch (Exception e)
        {
            if (RespuestaDTO.EsErrorInterno(e))
            {
                log.LogError(e, "Error en /Reportes");
            }
            return StatusCode(RespuestaDTO.CodigoHttp(e), RespuestaDTO.DeExcepcion(e));
        }
    }
}
