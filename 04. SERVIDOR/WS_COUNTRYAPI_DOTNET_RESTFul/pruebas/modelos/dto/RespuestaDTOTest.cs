using System.Data.Common;
using ec.edu.monster.modelos.enums;

namespace ec.edu.monster.modelos.dto;

/// <summary>
/// Código HTTP y mensaje de cada tipo de error (decisión A2 de TI.2.1, con las excepciones de .NET).
/// </summary>
public class RespuestaDTOTest
{
    /// <summary>Equivalente de SQLException: un error del proveedor de la base de datos.</summary>
    private sealed class ErrorDeBase(string mensaje) : DbException(mensaje);

    [Fact]
    public void CadaExcepcionTieneSuCodigoHttp()
    {
        Assert.Equal(400, RespuestaDTO.CodigoHttp(new ArgumentException("x")));
        Assert.Equal(401, RespuestaDTO.CodigoHttp(new UnauthorizedAccessException("x")));
        Assert.Equal(404, RespuestaDTO.CodigoHttp(new KeyNotFoundException("x")));
        Assert.Equal(409, RespuestaDTO.CodigoHttp(new InvalidOperationException("x")));
        Assert.Equal(502, RespuestaDTO.CodigoHttp(new IOException("x")));
        Assert.Equal(500, RespuestaDTO.CodigoHttp(new ErrorDeBase("x")));
        Assert.Equal(500, RespuestaDTO.CodigoHttp(new NullReferenceException()));
    }

    [Fact]
    public void LosErroresDeNegocioConservanSuMensaje()
    {
        RespuestaDTO<object> r = RespuestaDTO.DeExcepcion(new InvalidOperationException("Ya existe un país con ese nombre"));
        Assert.Equal(EstadoRespuesta.ERROR, r.Estado);
        Assert.Equal("Ya existe un país con ese nombre", r.Mensaje);
        Assert.Null(r.Datos);
    }

    [Fact]
    public void LosErroresDeBaseDeDatosNoExponenElSql()
    {
        RespuestaDTO<object> r = RespuestaDTO.DeExcepcion(new ErrorDeBase("Communications link failure: SELECT * FROM Paises"));
        Assert.Equal("Servicio no disponible, intente más tarde", r.Mensaje);
    }

    [Fact]
    public void LosErroresInesperadosUsanUnMensajeFijo()
    {
        RespuestaDTO<object> r = RespuestaDTO.DeExcepcion(new NullReferenceException("campo x es null"));
        Assert.Equal("Ocurrió un error inesperado, intente nuevamente", r.Mensaje);
    }
}
