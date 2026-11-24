using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.dto;
using ec.edu.monster.servicios;

namespace ec.edu.monster.controladores;

/// <summary>
/// Reglas de negocio de PaisControlador con DAO en memoria y una API falsa.
/// </summary>
public class PaisControladorTest
{
    /// <summary>API que devuelve la lista preparada por la prueba, sin conectarse a nada.</summary>
    private sealed class ApiFalsa(Func<List<Pais>> respuesta) : ConsumoAPI
    {
        public override Task<List<Pais>> ObtenerTodos() => Task.FromResult(respuesta());
        public override Task<List<Pais>> BuscarPorNombre(string nombre) => Task.FromResult(respuesta());
    }

    /// <summary>API caída: la importación falla antes de tocar la base.</summary>
    private sealed class ApiCaida : ConsumoAPI
    {
        public override Task<List<Pais>> ObtenerTodos() =>
            throw new IOException("La API REST Countries no está disponible, intente más tarde");
    }

    private readonly DaosFalsos.PaisDAOFalso paisDAO;
    private readonly PaisControlador controlador;
    /// <summary>Lo que devolverá la API falsa.</summary>
    private List<Pais> respuestaApi = [];

    public PaisControladorTest()
    {
        paisDAO = new DaosFalsos.PaisDAOFalso();
        paisDAO.Agregar("Ecuador", "América", 17_643_060);
        controlador = new PaisControlador(paisDAO, new DaosFalsos.RegionDAOFalso(), new ApiFalsa(() => respuestaApi));
    }

    private static Pais NuevoPais(string nombre, string? region, long poblacion, double area)
    {
        return new Pais(nombre, "Capital", region == null ? null : new Region(0, region), "Sub", poblacion, area, "Moneda");
    }

    private static void AssertMensaje<T>(string mensaje, Action accion) where T : Exception
    {
        var e = Assert.Throws<T>(accion);
        Assert.Equal(mensaje, e.Message);
    }

    [Fact]
    public void RegistraUnPaisValidoConLaRegionDelCatalogo()
    {
        Pais registrado = controlador.RegistrarPais(NuevoPais("  Perú  ", "América", 33_000_000, 1_285_216));
        Assert.True(registrado.Id > 0);
        Assert.Equal("Perú", registrado.Nombre);
        Assert.Equal(1, registrado.Region!.IdRegion);
    }

    [Fact]
    public void ValidaElNombre()
    {
        AssertMensaje<ArgumentException>(PaisControlador.NOMBRE_INVALIDO,
            () => controlador.RegistrarPais(NuevoPais("  ", "América", 1, 1)));
        AssertMensaje<ArgumentException>(PaisControlador.NOMBRE_INVALIDO,
            () => controlador.RegistrarPais(NuevoPais(new string('x', 101), "América", 1, 1)));
        AssertMensaje<ArgumentException>(PaisControlador.NOMBRE_INVALIDO,
            () => controlador.RegistrarPais(null));
    }

    [Fact]
    public void ValidaRegionPoblacionAreaYLargos()
    {
        AssertMensaje<ArgumentException>(PaisControlador.SELECCIONE_REGION,
            () => controlador.RegistrarPais(NuevoPais("Chile", null, 1, 1)));
        AssertMensaje<ArgumentException>(PaisControlador.REGION_NO_VALIDA,
            () => controlador.RegistrarPais(NuevoPais("Chile", "Atlántida", 1, 1)));
        AssertMensaje<ArgumentException>(PaisControlador.POBLACION_INVALIDA,
            () => controlador.RegistrarPais(NuevoPais("Chile", "América", -1, 1)));
        AssertMensaje<ArgumentException>(PaisControlador.AREA_INVALIDA,
            () => controlador.RegistrarPais(NuevoPais("Chile", "América", 1, -0.5)));
        Pais conCapitalLarga = NuevoPais("Chile", "América", 1, 1);
        conCapitalLarga.Capital = new string('c', 101);
        AssertMensaje<ArgumentException>(PaisControlador.TEXTOS_LARGOS,
            () => controlador.RegistrarPais(conCapitalLarga));
    }

    [Fact]
    public void AceptaPoblacionYAreaCero()
    {
        Assert.True(controlador.RegistrarPais(NuevoPais("Isla", "Antártida", 0, 0)).Id > 0);
    }

    [Fact]
    public void NoPermiteRegistrarUnNombreRepetido()
    {
        AssertMensaje<InvalidOperationException>(PaisControlador.NOMBRE_REPETIDO,
            () => controlador.RegistrarPais(NuevoPais("ecuador", "América", 1, 1)));
    }

    [Fact]
    public void ActualizarUnPaisInexistenteEsNoEncontrado()
    {
        AssertMensaje<KeyNotFoundException>(PaisControlador.PAIS_NO_EXISTE,
            () => controlador.ActualizarPais(999, NuevoPais("Chile", "América", 1, 1)));
    }

    [Fact]
    public void ActualizarConElNombreDeOtroPaisEsConflicto()
    {
        Pais colombia = paisDAO.Agregar("Colombia", "América", 50_000_000);
        AssertMensaje<InvalidOperationException>(PaisControlador.NOMBRE_REPETIDO,
            () => controlador.ActualizarPais(colombia.Id, NuevoPais("Ecuador", "América", 1, 1)));
    }

    [Fact]
    public void ActualizarConservandoSuPropioNombreFunciona()
    {
        Pais actualizado = controlador.ActualizarPais(1, NuevoPais("Ecuador", "América", 18_000_000, 276_841));
        Assert.Equal(18_000_000, actualizado.Poblacion);
    }

    [Fact]
    public void EliminarUnPaisInexistenteEsNoEncontrado()
    {
        controlador.EliminarPais(1);
        AssertMensaje<KeyNotFoundException>(PaisControlador.PAIS_NO_EXISTE, () => controlador.EliminarPais(1));
    }

    [Fact]
    public void BuscarConUnaRegionInexistenteEsRegionNoValida()
    {
        AssertMensaje<ArgumentException>(PaisControlador.REGION_NO_VALIDA,
            () => controlador.BuscarPaises(null, null, "Narnia"));
    }

    [Fact]
    public void BuscarFiltraPorRegionYSinCriteriosDevuelveTodo()
    {
        paisDAO.Agregar("España", "Europa", 47_000_000);
        // La región del país agregado se resuelve igual que en la base: por id del catálogo.
        foreach (Pais p in paisDAO.paises.Values)
        {
            p.AsignarRegion(new DaosFalsos.RegionDAOFalso().BuscarPorNombre(p.NombreRegion!));
        }
        Assert.Single(controlador.BuscarPaises(null, null, "Europa"));
        Assert.Equal(2, controlador.BuscarPaises("", " ", "").Count);
    }

    [Fact]
    public async Task ConsultaEnLaApiExigeDosCaracteres()
    {
        var e = await Assert.ThrowsAsync<ArgumentException>(() => controlador.ConsultarEnAPI(" e "));
        Assert.Equal(PaisControlador.NOMBRE_CORTO, e.Message);
        e = await Assert.ThrowsAsync<ArgumentException>(() => controlador.ConsultarEnAPI(null));
        Assert.Equal(PaisControlador.NOMBRE_CORTO, e.Message);
    }

    [Fact]
    public async Task ConsultaEnLaApiMarcaLosPaisesYaGuardados()
    {
        respuestaApi = [NuevoPais("Ecuador", "América", 1, 1), NuevoPais("Chile", "América", 1, 1)];
        List<PaisDTO> resultado = await controlador.ConsultarEnAPI("ec");
        Assert.True(resultado[0].Guardado);
        Assert.False(resultado[1].Guardado);
    }

    [Fact]
    public async Task ImportarAsignaLaRegionDelCatalogoYCuentaLosRecibidos()
    {
        respuestaApi =
        [
            NuevoPais("Ecuador", "América", 1, 1),
            NuevoPais("Chile", "América", 1, 1),
            NuevoPais("Japón", "Asia", 1, 1)
        ];
        ResumenImportacion resumen = await controlador.ImportarPaises();
        Assert.Equal(3, resumen.Recibidos);
        Assert.Equal(2, resumen.Registrados);
        Assert.Equal(1, resumen.Actualizados);
        Assert.Equal(3, paisDAO.ultimoLoteImportado![2].Region!.IdRegion);
    }

    [Fact]
    public async Task SiLaApiFallaNoSeTocaLaBase()
    {
        var conApiCaida = new PaisControlador(paisDAO, new DaosFalsos.RegionDAOFalso(), new ApiCaida());
        await Assert.ThrowsAsync<IOException>(() => conApiCaida.ImportarPaises());
        Assert.Null(paisDAO.ultimoLoteImportado);
    }
}
