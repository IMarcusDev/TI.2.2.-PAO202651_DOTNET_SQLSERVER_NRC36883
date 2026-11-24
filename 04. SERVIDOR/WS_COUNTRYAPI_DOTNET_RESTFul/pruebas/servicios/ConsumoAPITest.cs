using ec.edu.monster.modelos.data;

namespace ec.edu.monster.servicios;

/// <summary>
/// Mapeo del JSON de REST Countries v5 a los siete datos del taller y manejo de errores (ECUD8).
/// </summary>
public class ConsumoAPITest
{
    /// <summary>Ecuador tal como lo devuelve REST Countries v5 con los campos que pide el sistema.</summary>
    private const string ECUADOR = """
        {"names":{"common":"Ecuador","translations":{"spa":{"common":"Ecuador","official":"República del Ecuador"}}},
         "capitals":[{"attributes":{"primary":true},"coordinates":{"lat":-0.22,"lng":-78.5},"name":"Quito"}],
         "region":"Americas","subregion":"South America",
         "area":{"kilometers":276841,"miles":106888.9},
         "currencies":[{"code":"USD","name":"United States dollar","symbol":"$"}],
         "population":18135478}
        """;

    private readonly ConsumoAPI consumoAPI = new("http://no-se-usa", "clave-de-prueba");

    /// <summary>Envuelve los países como una página de la v5.</summary>
    private static string Pagina(bool hayMas, params string[] paises)
    {
        return "{\"data\":{\"objects\":[" + string.Join(",", paises) + "],\"meta\":{\"count\":"
            + paises.Length + ",\"more\":" + (hayMas ? "true" : "false") + "}}}";
    }

    [Fact]
    public void ConvierteLosSieteDatosDeUnPaisCompleto()
    {
        Pais pais = consumoAPI.ConvertirRespuesta(Pagina(false, ECUADOR))[0];
        Assert.Equal("Ecuador", pais.Nombre);
        Assert.Equal("Quito", pais.Capital);
        Assert.Equal("América", pais.NombreRegion);
        Assert.Equal("South America", pais.SubRegion);
        Assert.Equal(18135478L, pais.Poblacion);
        Assert.Equal(276841.0, pais.Area);
        Assert.Equal("United States dollar (USD)", pais.Moneda);
    }

    [Fact]
    public void UsaElNombreEnEspanolCuandoExiste()
    {
        const string alemania = """
            {"names":{"common":"Germany","translations":{"spa":{"common":"Alemania"}}},
             "capitals":[{"name":"Berlin"}],"region":"Europe","subregion":"Western Europe",
             "population":83240525,"area":{"kilometers":357114},"currencies":[{"code":"EUR","name":"Euro"}]}
            """;
        Pais pais = consumoAPI.ConvertirRespuesta(Pagina(false, alemania))[0];
        Assert.Equal("Alemania", pais.Nombre);
        // Sin primary, se toma la primera capital.
        Assert.Equal("Berlin", pais.Capital);
        Assert.Equal("Europa", pais.NombreRegion);
        Assert.Equal("Euro (EUR)", pais.Moneda);
    }

    [Fact]
    public void UsaElNombreComunSiNoHayTraduccionAlEspanol()
    {
        const string kosovo = """
            {"names":{"common":"Kosovo","translations":{}},"capitals":[{"name":"Pristina"}],
             "region":"Europe","subregion":"Southeast Europe","population":1775378,
             "area":{"kilometers":10908},"currencies":[{"code":"EUR","name":"Euro"}]}
            """;
        Assert.Equal("Kosovo", consumoAPI.ConvertirRespuesta(Pagina(false, kosovo))[0].Nombre);
    }

    [Fact]
    public void TomaLaCapitalMarcadaComoPrincipal()
    {
        const string sudafrica = """
            {"names":{"common":"South Africa","translations":{"spa":{"common":"Sudáfrica"}}},
             "capitals":[{"name":"Bloemfontein","attributes":{"primary":false,"judicial":true}},
                         {"name":"Pretoria","attributes":{"primary":true}},
                         {"name":"Cape Town","attributes":{"legislative":true}}],
             "region":"Africa","subregion":"Southern Africa","population":1,"area":{"kilometers":1},
             "currencies":[{"code":"ZAR","name":"South African rand"}]}
            """;
        Pais pais = consumoAPI.ConvertirRespuesta(Pagina(false, sudafrica))[0];
        Assert.Equal("Pretoria", pais.Capital);
        Assert.Equal("África", pais.NombreRegion);
    }

    [Fact]
    public void AplicaLosValoresPorDefectoCuandoFaltanDatos()
    {
        // Isla Bouvet: sin capital, sin subregión, sin moneda y sin área.
        const string bouvet = """
            {"names":{"common":"Bouvet Island","translations":{"spa":{"common":"Isla Bouvet"}}},
             "capitals":[],"region":"Antarctic","subregion":"","population":0,"currencies":[]}
            """;
        Pais pais = consumoAPI.ConvertirRespuesta(Pagina(false, bouvet))[0];
        Assert.Equal("Isla Bouvet", pais.Nombre);
        Assert.Equal("Sin capital", pais.Capital);
        Assert.Equal("Antártida", pais.NombreRegion);
        Assert.Equal("Sin subregión", pais.SubRegion);
        Assert.Equal(0.0, pais.Area);
        Assert.Equal("Sin moneda", pais.Moneda);
    }

    [Fact]
    public void TraduceLasSeisRegiones()
    {
        string[][] casos =
        [
            ["Americas", "América"], ["Europe", "Europa"], ["Asia", "Asia"],
            ["Africa", "África"], ["Oceania", "Oceanía"], ["Antarctic", "Antártida"]
        ];
        foreach (string[] caso in casos)
        {
            string json = Pagina(false, "{\"names\":{\"common\":\"X\"},\"region\":\"" + caso[0] + "\",\"population\":1}");
            Assert.Equal(caso[1], consumoAPI.ConvertirRespuesta(json)[0].NombreRegion);
        }
    }

    [Fact]
    public void DescartaLosPaisesConRegionDesconocida()
    {
        const string atlantis = "{\"names\":{\"common\":\"Atlantis\"},\"region\":\"Ocean\",\"population\":5}";
        List<Pais> paises = consumoAPI.ConvertirRespuesta(Pagina(false, ECUADOR, atlantis));
        Assert.Single(paises);
        Assert.Equal("Ecuador", paises[0].Nombre);
    }

    [Fact]
    public void RechazaUnJsonSinDataObjects()
    {
        var e = Assert.Throws<IOException>(
            () => consumoAPI.ConvertirRespuesta("{\"errors\":[{\"message\":\"Not found\"}]}"));
        Assert.Equal(ConsumoAPI.RESPUESTA_NO_VALIDA, e.Message);
        Assert.Throws<IOException>(() => consumoAPI.ConvertirRespuesta("[" + ECUADOR + "]"));
        Assert.Throws<IOException>(() => consumoAPI.ConvertirRespuesta("<html>error</html>"));
        Assert.Throws<IOException>(() => consumoAPI.ConvertirRespuesta(""));
    }

    [Fact]
    public async Task ObtenerTodosRecorreLasPaginasYEnviaLaClave()
    {
        using var servidor = new ServidorFalso();
        // Página 1 (offset=0) con más páginas; página 2 (offset=1) es la última.
        servidor.ResponderSegun(ruta => ruta.Contains("offset=0")
            ? Pagina(true, ECUADOR)
            : Pagina(false, ECUADOR
                .Replace("\"common\":\"Ecuador\",\"translations\"", "\"common\":\"Chile\",\"translations\"")
                .Replace("República del Ecuador", "Chile")
                .Replace("\"spa\":{\"common\":\"Ecuador\"", "\"spa\":{\"common\":\"Chile\"")));
        List<Pais> paises = await new ConsumoAPI(servidor.Url(), "rc_prueba").ObtenerTodos();
        Assert.Equal(2, paises.Count);
        Assert.Equal("Chile", paises[1].Nombre);
        List<string> rutas = servidor.Rutas();
        Assert.Equal(2, rutas.Count);
        Assert.StartsWith("/?limit=100&offset=0&response_fields=", rutas[0]);
        Assert.StartsWith("/?limit=100&offset=1&", rutas[1]);
        Assert.Equal("Bearer rc_prueba", servidor.UltimoAuthorization());
    }

    [Fact]
    public async Task BuscarPorNombreUsaElAgregadoNameYCodificaElTexto()
    {
        using var servidor = new ServidorFalso();
        servidor.Responder(200, Pagina(false, ECUADOR));
        await new ConsumoAPI(servidor.Url(), "rc_prueba").BuscarPorNombre("costa rica");
        Assert.StartsWith("/name?q=costa+rica&limit=100&response_fields=", servidor.UltimaRuta());
    }

    [Fact]
    public async Task BuscarPorNombreSinCoincidenciasEsNoEncontrado()
    {
        using var servidor = new ServidorFalso();
        servidor.Responder(200, Pagina(false));
        var e = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => new ConsumoAPI(servidor.Url(), "rc_prueba").BuscarPorNombre("zzzz"));
        Assert.Equal(ConsumoAPI.SIN_COINCIDENCIAS, e.Message);
    }

    [Fact]
    public async Task UnaClaveRechazadaEsApiNoDisponible()
    {
        using var servidor = new ServidorFalso();
        servidor.Responder(401, "{\"errors\":[{\"message\":\"Authorization key required.\"}]}");
        var e = await Assert.ThrowsAsync<IOException>(() => new ConsumoAPI(servidor.Url(), "rc_mala").ObtenerTodos());
        Assert.Equal(ConsumoAPI.NO_DISPONIBLE, e.Message);
    }

    [Fact]
    public async Task SinClaveNoSeLlamaALaApi()
    {
        using var servidor = new ServidorFalso();
        var e = await Assert.ThrowsAsync<IOException>(() => new ConsumoAPI(servidor.Url(), " ").ObtenerTodos());
        Assert.Equal(ConsumoAPI.NO_DISPONIBLE, e.Message);
        Assert.Empty(servidor.Rutas());
    }

    [Fact]
    public async Task SinConexionEsApiNoDisponible()
    {
        // Puerto 9 (discard): no hay nada escuchando, la conexión se rechaza.
        var e = await Assert.ThrowsAsync<IOException>(
            () => new ConsumoAPI("http://127.0.0.1:9", "rc_prueba").ObtenerTodos());
        Assert.Equal(ConsumoAPI.NO_DISPONIBLE, e.Message);
    }
}
