using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.enums;

namespace ec.edu.monster.servicios;

/// <summary>
/// Generación, validación, renovación, expiración y eliminación de tokens.
/// </summary>
public class TokenServiceTest
{
    /// <summary>Reloj que avanza solo cuando la prueba lo pide.</summary>
    private sealed class RelojManual : TimeProvider
    {
        private DateTimeOffset ahora = DateTimeOffset.Parse("2026-11-23T10:00:00Z");

        public void Avanzar(TimeSpan tiempo) => ahora = ahora.Add(tiempo);

        public override DateTimeOffset GetUtcNow() => ahora;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private readonly RelojManual reloj = new();
    private readonly TokenService tokenService;
    private readonly Usuario usuario = new(1, "monster", "hash", "Marcos Escobar", EstadoUsuario.ACTIVO);

    public TokenServiceTest()
    {
        tokenService = new TokenService(reloj);
    }

    [Fact]
    public void GeneraUnUuidYLoValida()
    {
        string token = tokenService.GenerarToken(usuario);
        Assert.True(Guid.TryParse(token, out _));
        Assert.Equal("Marcos Escobar", tokenService.ValidarToken(token)!.Nombre);
    }

    [Fact]
    public void AceptaElTokenConOSinPrefijoBearer()
    {
        string token = tokenService.GenerarToken(usuario);
        Assert.NotNull(tokenService.ValidarToken("Bearer " + token));
        Assert.NotNull(tokenService.ValidarToken("bearer " + token));
        Assert.NotNull(tokenService.ValidarToken("  " + token + "  "));
    }

    [Fact]
    public void RechazaTokenAusenteVacioODesconocido()
    {
        Assert.Null(tokenService.ValidarToken(null));
        Assert.Null(tokenService.ValidarToken(""));
        Assert.Null(tokenService.ValidarToken("Bearer "));
        Assert.Null(tokenService.ValidarToken(Guid.NewGuid().ToString()));
    }

    [Fact]
    public void SigueValidoAlCumplirExactamente15Minutos()
    {
        string token = tokenService.GenerarToken(usuario);
        reloj.Avanzar(TimeSpan.FromMinutes(15));
        Assert.NotNull(tokenService.ValidarToken(token));
    }

    [Fact]
    public void ExpiraDespuesDe15MinutosSinUsoYSeElimina()
    {
        string token = tokenService.GenerarToken(usuario);
        reloj.Avanzar(TimeSpan.FromMinutes(15).Add(TimeSpan.FromSeconds(1)));
        Assert.Null(tokenService.ValidarToken(token));
        // Ya se eliminó: aunque el reloj no cambie, sigue siendo inválido.
        Assert.Null(tokenService.ValidarToken(token));
    }

    [Fact]
    public void CadaUsoRenuevaElTiempo()
    {
        string token = tokenService.GenerarToken(usuario);
        reloj.Avanzar(TimeSpan.FromMinutes(10));
        Assert.NotNull(tokenService.ValidarToken(token));
        reloj.Avanzar(TimeSpan.FromMinutes(10));
        // 20 minutos en total, pero solo 10 sin uso.
        Assert.NotNull(tokenService.ValidarToken(token));
    }

    [Fact]
    public void EliminarTokenCierraLaSesion()
    {
        string token = tokenService.GenerarToken(usuario);
        tokenService.EliminarToken("Bearer " + token);
        Assert.Null(tokenService.ValidarToken(token));
        // Eliminar un token inexistente no falla.
        tokenService.EliminarToken(token);
        tokenService.EliminarToken(null);
    }
}
