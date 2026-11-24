using ec.edu.monster.modelos.data;

namespace ec.edu.monster.servicios;

/// <summary>
/// Mapeo del JSON de la API de universidades (ECUD11) y manejo de errores.
/// </summary>
public class ConsumoAPIUniversidadesTest
{
    private const string ESPE = """
        {"name":"Escuela Politécnica del Ejército","domains":["espe.edu.ec","espe.ec"],
         "web_pages":["http://www.espe.edu.ec/"],"country":"Ecuador","alpha_two_code":"EC",
         "state-province":null}
        """;

    private readonly ConsumoAPIUniversidades consumo = new("http://no-se-usa");

    [Fact]
    public void TomaNombrePrimerDominioPrimerSitioYPais()
    {
        Universidad u = consumo.ConvertirRespuesta("[" + ESPE + "]", "Ecuador")[0];
        Assert.Equal("Escuela Politécnica del Ejército", u.Nombre);
        Assert.Equal("espe.edu.ec", u.Dominio);
        Assert.Equal("http://www.espe.edu.ec/", u.SitioWeb);
        Assert.Equal("Ecuador", u.Pais);
    }

    [Fact]
    public void UsaValoresPorDefectoSiFaltanDominioOSitio()
    {
        const string json = "[{\"name\":\"Universidad X\",\"domains\":[],\"web_pages\":[],\"country\":\"Ecuador\"}]";
        Universidad u = consumo.ConvertirRespuesta(json, "Ecuador")[0];
        Assert.Equal("Sin dominio", u.Dominio);
        Assert.Equal("Sin sitio web", u.SitioWeb);
    }

    [Fact]
    public void DescartaLasRepetidasConElMismoNombre()
    {
        List<Universidad> lista = consumo.ConvertirRespuesta("[" + ESPE + "," + ESPE + "]", "Ecuador");
        Assert.Single(lista);
    }

    [Fact]
    public void ListaVaciaSiLaApiNoTieneUniversidades()
    {
        Assert.Empty(consumo.ConvertirRespuesta("[]", "Narnia"));
    }

    [Fact]
    public void UnJsonNoValidoEsApiNoDisponible()
    {
        var e = Assert.Throws<IOException>(() => consumo.ConvertirRespuesta("{}", "Ecuador"));
        Assert.Equal(ConsumoAPIUniversidades.NO_DISPONIBLE, e.Message);
    }

    [Fact]
    public async Task CodificaElPaisYSigueRedirecciones()
    {
        using var servidor = new ServidorFalso();
        servidor.Responder(200, "[" + ESPE + "]");
        servidor.RedirigirUnaVez(servidor.Url() + "/nuevo/search?country=Costa+Rica");
        List<Universidad> lista = await new ConsumoAPIUniversidades(servidor.Url()).ObtenerPorPais("Costa Rica");
        Assert.Single(lista);
        Assert.Equal("/nuevo/search?country=Costa+Rica", servidor.UltimaRuta());
    }

    [Fact]
    public async Task UnErrorDeLaApiEsApiNoDisponible()
    {
        using var servidor = new ServidorFalso();
        servidor.Responder(503, "caído");
        var e = await Assert.ThrowsAsync<IOException>(
            () => new ConsumoAPIUniversidades(servidor.Url()).ObtenerPorPais("Ecuador"));
        Assert.Equal(ConsumoAPIUniversidades.NO_DISPONIBLE, e.Message);
    }
}
