using ec.edu.monster.config;
using ec.edu.monster.dobles;
using ec.edu.monster.modelos;
using ec.edu.monster.servicios;

namespace ec.edu.monster.pruebas;

/// <summary>
/// Lo propio del cliente móvil: la dirección del servidor que el usuario escribe en la app y el
/// cambio de servidor sin reiniciar.
/// </summary>
public class ConfigServidorTest
{
    [Theory]
    [InlineData("http://10.0.2.2:5088/api")]
    [InlineData("http://192.168.1.20:5088/api/")]
    [InlineData("https://servidor.espe.edu.ec/api")]
    [InlineData("  http://localhost:5088/api  ")]
    public void AceptaDireccionesHttpConEquipo(string url)
    {
        Assert.True(ConfigServidor.EsUrlValida(url));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("192.168.1.20:5088/api")]
    [InlineData("ftp://192.168.1.20/api")]
    [InlineData("http://")]
    [InlineData("http:// /api")]
    [InlineData("localhost")]
    public void RechazaDireccionesSinHttpOSinEquipo(string url)
    {
        Assert.False(ConfigServidor.EsUrlValida(url));
    }

    [Fact]
    public void RechazaNull()
    {
        Assert.False(ConfigServidor.EsUrlValida(null));
    }

    [Fact]
    public void QuitaLasBarrasDelFinal()
    {
        Assert.Equal("http://10.0.2.2:5088/api", ConfigServidor.QuitarBarraFinal("http://10.0.2.2:5088/api//"));
        Assert.Equal("http://10.0.2.2:5088/api", ConfigServidor.QuitarBarraFinal("http://10.0.2.2:5088/api"));
    }

    [Fact]
    public void LaDireccionPorDefectoEsLaDelEmuladorSinBarraFinal()
    {
        Assert.Equal("http://10.0.2.2:5088/api", ConfigServidor.URL_POR_DEFECTO);
        Assert.Equal(15, ConfigServidor.TIMEOUT_SEGUNDOS);
    }

    [Fact]
    public async Task CambiarElServidorAfectaALasPeticionesSiguientes()
    {
        using var primero = new ServidorFalso();
        using var segundo = new ServidorFalso();
        primero.Ok("GET /Paises", "Consulta realizada correctamente", "[]");
        segundo.Ok("GET /Paises", "Consulta realizada correctamente",
            "[{\"id\":1,\"nombre\":\"Ecuador\",\"region\":\"América\",\"poblacion\":17643060}]");
        var cliente = new ClienteRest(primero.UrlBase() + "/");
        var servicio = new PaisServicio(cliente);
        Assert.Equal(primero.UrlBase(), cliente.UrlBase);

        RespuestaDTO<List<Pais>> antes = await servicio.Listar("t", null);
        cliente.UrlBase = segundo.UrlBase();
        RespuestaDTO<List<Pais>> despues = await servicio.Listar("t", null);

        Assert.Empty(antes.Datos!);
        Assert.Equal("Ecuador", despues.Datos![0].Nombre);
        Assert.Single(primero.Peticiones());
        Assert.Single(segundo.Peticiones());
    }

    [Fact]
    public void CodificarDejaLosEspaciosComoPorcentaje20()
    {
        Assert.Equal("Costa%20Rica", ClienteRest.Codificar("Costa Rica"));
        Assert.Equal("Per%C3%BA", ClienteRest.Codificar("Perú"));
        Assert.Equal("a%26b%3Dc", ClienteRest.Codificar("a&b=c"));
    }
}
