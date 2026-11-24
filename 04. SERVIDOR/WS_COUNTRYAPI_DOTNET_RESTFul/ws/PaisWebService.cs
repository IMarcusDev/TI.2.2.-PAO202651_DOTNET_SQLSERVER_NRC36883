using ec.edu.monster.controladores;
using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.dto;
using ec.edu.monster.servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ec.edu.monster.ws;

/// <summary>
/// Servicio de países: /api/Paises (ECUD2 a ECUD5). Todas las operaciones exigen un token
/// válido en el encabezado Authorization.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
[Route("api/Paises")]
[Produces("application/json")]
public class PaisWebService : ControllerBase
{
    private readonly PaisControlador controlador;
    private readonly TokenService tokenService;
    private readonly ILogger<PaisWebService> log;

    public PaisWebService(PaisControlador controlador, TokenService tokenService, ILogger<PaisWebService> log)
    {
        this.controlador = controlador;
        this.tokenService = tokenService;
        this.log = log;
    }

    /// <summary>GET /api/Paises[?nombre=texto]: listado ordenado por nombre, o búsqueda por nombre.</summary>
    [HttpGet]
    public IActionResult Listar([FromHeader(Name = "Authorization")] string? authorization,
        [FromQuery] string? nombre)
    {
        if (tokenService.ValidarToken(authorization) == null)
        {
            return TokenInvalido();
        }
        try
        {
            List<Pais> paises = controlador.BuscarPorNombre(nombre);
            return Ok(RespuestaDTO.Ok("Consulta realizada correctamente", ADTO(paises)));
        }
        catch (Exception e)
        {
            return Error(e);
        }
    }

    /// <summary>POST /api/Paises: registra un país (201) o responde 400 / 409.</summary>
    [HttpPost]
    public IActionResult Registrar([FromHeader(Name = "Authorization")] string? authorization,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PaisDTO? pais)
    {
        if (tokenService.ValidarToken(authorization) == null)
        {
            return TokenInvalido();
        }
        try
        {
            Pais registrado = controlador.RegistrarPais(pais?.APais());
            return StatusCode(201, RespuestaDTO.Ok("País registrado correctamente", PaisDTO.Desde(registrado, true)));
        }
        catch (Exception e)
        {
            return Error(e);
        }
    }

    /// <summary>PUT /api/Paises/{id}: actualiza el país o responde 400 / 404 / 409.</summary>
    [HttpPut("{id:int}")]
    public IActionResult Actualizar([FromHeader(Name = "Authorization")] string? authorization,
        [FromRoute] int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PaisDTO? pais)
    {
        if (tokenService.ValidarToken(authorization) == null)
        {
            return TokenInvalido();
        }
        try
        {
            Pais actualizado = controlador.ActualizarPais(id, pais?.APais());
            return Ok(RespuestaDTO.Ok("País actualizado correctamente", PaisDTO.Desde(actualizado, true)));
        }
        catch (Exception e)
        {
            return Error(e);
        }
    }

    /// <summary>DELETE /api/Paises/{id}: elimina el país o responde 404.</summary>
    [HttpDelete("{id:int}")]
    public IActionResult Eliminar([FromHeader(Name = "Authorization")] string? authorization, [FromRoute] int id)
    {
        if (tokenService.ValidarToken(authorization) == null)
        {
            return TokenInvalido();
        }
        try
        {
            controlador.EliminarPais(id);
            return Ok(RespuestaDTO.Ok<object>("País eliminado correctamente", null));
        }
        catch (Exception e)
        {
            return Error(e);
        }
    }

    /// <summary>GET /api/Paises/buscar?nombre=&amp;capital=&amp;region=: criterios combinables.</summary>
    [HttpGet("buscar")]
    public IActionResult Buscar([FromHeader(Name = "Authorization")] string? authorization,
        [FromQuery] string? nombre, [FromQuery] string? capital, [FromQuery] string? region)
    {
        if (tokenService.ValidarToken(authorization) == null)
        {
            return TokenInvalido();
        }
        try
        {
            List<Pais> paises = controlador.BuscarPaises(nombre, capital, region);
            return Ok(RespuestaDTO.Ok("Búsqueda realizada correctamente", ADTO(paises)));
        }
        catch (Exception e)
        {
            return Error(e);
        }
    }

    /// <summary>POST /api/Paises/importar: importa todos los países de REST Countries.</summary>
    [HttpPost("importar")]
    public async Task<IActionResult> Importar([FromHeader(Name = "Authorization")] string? authorization)
    {
        if (tokenService.ValidarToken(authorization) == null)
        {
            return TokenInvalido();
        }
        try
        {
            return Ok(RespuestaDTO.Ok("Importación finalizada", await controlador.ImportarPaises()));
        }
        catch (Exception e)
        {
            return Error(e);
        }
    }

    /// <summary>GET /api/Paises/externo/{nombre}: consulta el país en REST Countries sin guardarlo.</summary>
    [HttpGet("externo/{nombre}")]
    public async Task<IActionResult> ConsultarExterno([FromHeader(Name = "Authorization")] string? authorization,
        [FromRoute] string nombre)
    {
        if (tokenService.ValidarToken(authorization) == null)
        {
            return TokenInvalido();
        }
        try
        {
            return Ok(RespuestaDTO.Ok("Consulta en la API exitosa", await controlador.ConsultarEnAPI(nombre)));
        }
        catch (Exception e)
        {
            return Error(e);
        }
    }

    /// <summary>POST /api/Paises/guardar: guarda el país elegido de la consulta a la API (registra o actualiza).</summary>
    [HttpPost("guardar")]
    public IActionResult Guardar([FromHeader(Name = "Authorization")] string? authorization,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PaisDTO? pais)
    {
        if (tokenService.ValidarToken(authorization) == null)
        {
            return TokenInvalido();
        }
        try
        {
            Pais guardado = controlador.GuardarDesdeAPI(pais?.APais());
            return Ok(RespuestaDTO.Ok("País guardado correctamente", PaisDTO.Desde(guardado, true)));
        }
        catch (Exception e)
        {
            return Error(e);
        }
    }

    private static List<PaisDTO> ADTO(List<Pais> paises)
    {
        return paises.Select(p => PaisDTO.Desde(p, true)).ToList();
    }

    private ObjectResult TokenInvalido()
    {
        return StatusCode(401, RespuestaDTO.Error(RespuestaDTO.TOKEN_INVALIDO));
    }

    private ObjectResult Error(Exception e)
    {
        if (RespuestaDTO.EsErrorInterno(e))
        {
            log.LogError(e, "Error en /Paises");
        }
        return StatusCode(RespuestaDTO.CodigoHttp(e), RespuestaDTO.DeExcepcion(e));
    }
}
