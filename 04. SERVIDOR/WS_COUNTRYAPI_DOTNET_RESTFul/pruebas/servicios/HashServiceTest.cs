namespace ec.edu.monster.servicios;

/// <summary>
/// El hash debe coincidir con el que guarda seed.sql para los usuarios de prueba.
/// </summary>
public class HashServiceTest
{
    private readonly HashService hashService = new();

    [Fact]
    public void HashDeMonster9CoincideConSeed()
    {
        Assert.Equal("774e993500f4027acfd72b7a7ee564b76ae43cf7c4c943ed0e0f364cca16b6ec",
            hashService.Sha256("monster9"));
    }

    [Fact]
    public void HashDeAdmin123CoincideConSeed()
    {
        Assert.Equal("240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9",
            hashService.Sha256("admin123"));
    }

    [Fact]
    public void HashTiene64CaracteresEnMinusculas()
    {
        string hash = hashService.Sha256("Clave con tildes: áéí");
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash.ToLowerInvariant(), hash);
    }
}
