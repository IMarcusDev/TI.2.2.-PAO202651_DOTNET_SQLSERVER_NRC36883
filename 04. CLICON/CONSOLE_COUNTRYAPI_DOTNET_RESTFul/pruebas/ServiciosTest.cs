using ec.edu.monster.dobles;
using ec.edu.monster.modelos;
using ec.edu.monster.servicios;

namespace ec.edu.monster.pruebas;

/// <summary>
/// Servicios y ClienteRest contra un servidor falso: rutas, encabezados, cuerpo JSON y
/// manejo de errores (401, sin conexión, respuesta no válida).
/// </summary>
public class ServiciosTest : IDisposable
{
    private readonly ServidorFalso servidor = new();
    private readonly ClienteRest cliente;

    public ServiciosTest()
    {
        cliente = new ClienteRest(servidor.UrlBase(), 5);
    }

    public void Dispose()
    {
        servidor.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task LoginEnviaLasCredencialesYLeeElToken()
    {
        servidor.Ok("POST /Login/login", "Ingreso de sesión exitoso", "{\"token\":\"t-1\",\"nombre\":\"Marcos Escobar\"}");
        RespuestaDTO<LoginResponse> r = await new LoginServicio(cliente).Login("monster", "monster9");
        Assert.True(r.EsOk());
        Assert.Equal("t-1", r.Datos!.Token);
        Assert.Equal("Marcos Escobar", r.Datos.Nombre);
        Assert.Equal("{\"usuario\":\"monster\",\"clave\":\"monster9\"}", servidor.Ultima()!.Cuerpo);
        // El login no lleva token.
        Assert.Null(servidor.Ultima()!.Authorization);
    }

    [Fact]
    public async Task CredencialesIncorrectasConservanElMensajeDelServidor()
    {
        servidor.Error("POST /Login/login", 401, "Credenciales incorrectas");
        RespuestaDTO<LoginResponse> r = await new LoginServicio(cliente).Login("monster", "x");
        Assert.False(r.EsOk());
        Assert.Equal("Credenciales incorrectas", r.Mensaje);
        // Sin token, un 401 no es una sesión expirada.
        Assert.False(r.SesionExpirada);
    }

    [Fact]
    public async Task Un401ConTokenEsSesionExpirada()
    {
        servidor.Error("GET /Paises", 401, "Token inválido o sesión expirada");
        RespuestaDTO<List<Pais>> r = await new PaisServicio(cliente).Listar("t-1", null);
        Assert.True(r.SesionExpirada);
        Assert.Equal(ClienteRest.SESION_EXPIRADA, r.Mensaje);
        Assert.Equal("Bearer t-1", servidor.Ultima()!.Authorization);
    }

    [Fact]
    public async Task ListarConviertePaisesConTildes()
    {
        servidor.Ok("GET /Paises", "Consulta realizada correctamente",
            "[{\"id\":3,\"nombre\":\"Perú\",\"capital\":\"Lima\",\"region\":\"América\",\"subregion\":\"South America\","
            + "\"poblacion\":32971846,\"area\":1285216.0,\"moneda\":\"Peruvian sol (PEN)\",\"guardado\":true}]");
        RespuestaDTO<List<Pais>> r = await new PaisServicio(cliente).Listar("t-1", "per ú");
        Pais peru = r.Datos![0];
        Assert.Equal("Perú", peru.Nombre);
        Assert.Equal(32971846L, peru.Poblacion);
        Assert.Equal("/Paises?nombre=per%20%C3%BA", servidor.Ultima()!.Ruta);
    }

    [Fact]
    public async Task BuscarEnviaSoloLosCriteriosConValor()
    {
        servidor.Ok("GET /Paises/buscar", "Búsqueda realizada correctamente", "[]");
        await new PaisServicio(cliente).Buscar("t-1", new CriteriosBusqueda(" ", "quito", "América"));
        Assert.Equal("/Paises/buscar?capital=quito&region=Am%C3%A9rica", servidor.Ultima()!.Ruta);
        await new PaisServicio(cliente).Buscar("t-1", new CriteriosBusqueda(null, null, null));
        Assert.Equal("/Paises/buscar", servidor.Ultima()!.Ruta);
    }

    [Fact]
    public async Task RegistrarEnviaElPaisComoJsonEnUtf8()
    {
        servidor.Responder("POST /Paises", 201,
            "{\"estado\":\"OK\",\"mensaje\":\"País registrado correctamente\",\"datos\":{\"id\":20,\"nombre\":\"Japón\"}}");
        RespuestaDTO<Pais> r = await new PaisServicio(cliente).Registrar("t-1",
            new Pais("Japón", "Tokyo", "Asia", "Eastern Asia", 1, 2.5, "Yen"));
        Assert.Equal(20, r.Datos!.Id);
        Assert.Equal(201, r.CodigoHttp);
        Assert.Contains("\"nombre\":\"Japón\"", servidor.Ultima()!.Cuerpo);
    }

    [Fact]
    public async Task EliminarYConsultarExternoUsanLaRutaCorrecta()
    {
        servidor.Ok("DELETE /Paises/7", "País eliminado correctamente", "null");
        Assert.True((await new PaisServicio(cliente).Eliminar("t-1", 7)).EsOk());
        servidor.Ok("GET /Paises/externo/costa rica", "Consulta en la API exitosa", "[]");
        await new PaisServicio(cliente).ConsultarExterno("t-1", "costa rica");
        Assert.Equal("/Paises/externo/costa%20rica", servidor.Ultima()!.Ruta);
    }

    [Fact]
    public async Task ReporteConvierteLosTresReportes()
    {
        servidor.Ok("GET /Reportes", "Reportes generados correctamente",
            "{\"paisesPorRegion\":[{\"region\":\"América\",\"cantidad\":4}],"
            + "\"paisMasPoblado\":{\"nombre\":\"China\",\"poblacion\":1402112000},"
            + "\"promedioPorRegion\":[{\"region\":\"Asia\",\"promedioPoblacion\":969317469}]}");
        Reporte reporte = (await new ReporteServicio(cliente).Generar("t-1")).Datos!;
        Assert.Equal(4, reporte.PaisesPorRegion[0].Cantidad);
        Assert.Equal("China", reporte.PaisMasPoblado!.Nombre);
        Assert.Equal(969317469L, reporte.PromedioPorRegion[0].PromedioPoblacion);
    }

    [Fact]
    public async Task SinConexionDaElMensajeDelEcud()
    {
        var sinServidor = new ClienteRest("http://127.0.0.1:9/api", 2);
        RespuestaDTO<List<Pais>> r = await new PaisServicio(sinServidor).Listar("t-1", null);
        Assert.False(r.EsOk());
        Assert.Equal("No se pudo conectar con el servidor. Verifique la red e intente nuevamente.", r.Mensaje);
        Assert.False(await sinServidor.ServidorDisponible());
    }

    [Fact]
    public async Task UnaRespuestaQueNoEsJsonEsUnErrorInesperado()
    {
        servidor.Responder("GET /Reportes", 500, "<html>Error</html>");
        RespuestaDTO<Reporte> r = await new ReporteServicio(cliente).Generar("t-1");
        Assert.False(r.EsOk());
        Assert.Equal(ClienteRest.RESPUESTA_NO_VALIDA, r.Mensaje);
    }

    [Fact]
    public async Task ElServidorFalsoSeReportaDisponible()
    {
        Assert.True(await cliente.ServidorDisponible());
    }
}
