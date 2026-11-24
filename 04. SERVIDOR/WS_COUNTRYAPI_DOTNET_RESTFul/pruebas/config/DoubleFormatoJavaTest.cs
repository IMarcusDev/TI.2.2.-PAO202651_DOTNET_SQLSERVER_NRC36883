namespace ec.edu.monster.config;

/// <summary>
/// Prueba propia de la versión .NET (no existe en Java): los double se escriben con el mismo texto
/// que Double.toString de Java. Los valores esperados se copiaron de respuestas reales del servidor
/// Java (DUDAS.md, H8).
/// </summary>
public class DoubleFormatoJavaTest
{
    [Theory]
    [InlineData(276841.0, "276841.0")]
    [InlineData(0.5, "0.5")]
    [InlineData(0.44, "0.44")]
    [InlineData(102.0, "102.0")]
    [InlineData(250.5, "250.5")]
    [InlineData(9999999.0, "9999999.0")]
    [InlineData(0.0, "0.0")]
    public void DecimalesEntreMilesimaYDiezMillones(double valor, string esperado)
    {
        Assert.Equal(esperado, DoubleFormatoJava.ComoJava(valor));
    }

    [Theory]
    [InlineData(17098242.0, "1.7098242E7")]
    [InlineData(14000000.0, "1.4E7")]
    [InlineData(10000000.0, "1.0E7")]
    [InlineData(0.0001, "1.0E-4")]
    [InlineData(0.00012345, "1.2345E-4")]
    [InlineData(1.5e20, "1.5E20")]
    public void NotacionCientificaFueraDeEseRango(double valor, string esperado)
    {
        Assert.Equal(esperado, DoubleFormatoJava.ComoJava(valor));
    }

    [Fact]
    public void ConservaElSigno()
    {
        Assert.Equal("-2.5", DoubleFormatoJava.ComoJava(-2.5));
        Assert.Equal("-1.2E8", DoubleFormatoJava.ComoJava(-1.2e8));
    }
}
