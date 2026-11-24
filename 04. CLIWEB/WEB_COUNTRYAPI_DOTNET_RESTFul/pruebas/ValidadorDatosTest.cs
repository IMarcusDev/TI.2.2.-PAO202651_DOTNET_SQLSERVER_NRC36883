using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.pruebas;

/// <summary>
/// Validaciones del ECUD: nombre obligatorio de hasta 100 caracteres, población entera mayor o
/// igual que cero, área mayor o igual que cero y consulta de al menos dos caracteres.
/// </summary>
public class ValidadorDatosTest
{
    private static Pais NuevoPais(string? nombre, string? region, long poblacion, double area)
    {
        return new Pais(nombre, "Capital", region, "Sub", poblacion, area, "Moneda");
    }

    [Fact]
    public void Credenciales()
    {
        Assert.Equal("Ingrese el usuario", ValidadorDatos.ValidarCredenciales("  ", "x"));
        Assert.Equal("Ingrese la clave", ValidadorDatos.ValidarCredenciales("monster", ""));
        Assert.Null(ValidadorDatos.ValidarCredenciales("monster", "monster9"));
    }

    [Fact]
    public void ConsultaEnLaApiConAlMenosDosCaracteres()
    {
        Assert.Equal("Ingrese al menos dos caracteres", ValidadorDatos.ValidarNombreConsulta(null));
        Assert.Equal("Ingrese al menos dos caracteres", ValidadorDatos.ValidarNombreConsulta(" e "));
        Assert.Null(ValidadorDatos.ValidarNombreConsulta("ec"));
    }

    [Fact]
    public void NombreObligatorioYDeHasta100Caracteres()
    {
        Assert.Equal("Ingrese el nombre del país", ValidadorDatos.ValidarPais(NuevoPais(" ", "Asia", 1, 1)));
        Assert.Equal("Ingrese el nombre del país", ValidadorDatos.ValidarPais(NuevoPais(new string('x', 101), "Asia", 1, 1)));
        Assert.Null(ValidadorDatos.ValidarPais(NuevoPais(new string('x', 100), "Asia", 1, 1)));
    }

    [Fact]
    public void RegionDeLaLista()
    {
        Assert.Equal("Seleccione una región", ValidadorDatos.ValidarPais(NuevoPais("Chile", null, 1, 1)));
        Assert.Equal("Seleccione una región", ValidadorDatos.ValidarPais(NuevoPais("Chile", "Narnia", 1, 1)));
    }

    [Fact]
    public void PoblacionYAreaMayoresOIgualesQueCero()
    {
        Assert.Equal("La población debe ser un número entero mayor o igual que cero",
            ValidadorDatos.ValidarPais(NuevoPais("Chile", "América", -1, 1)));
        Assert.Equal("El área debe ser un número mayor o igual que cero",
            ValidadorDatos.ValidarPais(NuevoPais("Chile", "América", 1, -0.1)));
        Assert.Equal("El área debe ser un número mayor o igual que cero",
            ValidadorDatos.ValidarPais(NuevoPais("Chile", "América", 1, double.NaN)));
        Assert.Null(ValidadorDatos.ValidarPais(NuevoPais("Isla", "Antártida", 0, 0)));
    }

    [Fact]
    public void TextosOpcionalesDeHasta100Caracteres()
    {
        Pais p = NuevoPais("Chile", "América", 1, 1);
        p.Moneda = new string('m', 101);
        Assert.Equal("La capital, la subregión y la moneda deben tener como máximo 100 caracteres",
            ValidadorDatos.ValidarPais(p));
    }

    [Fact]
    public void PaisDeUniversidadesObligatorio()
    {
        Assert.Equal("Ingrese el país", ValidadorDatos.ValidarPaisUniversidades(" "));
        Assert.Null(ValidadorDatos.ValidarPaisUniversidades("Ecuador"));
    }

    [Fact]
    public void LeePoblacionConOSinSeparadoresDeMiles()
    {
        Assert.Equal(17643060L, ValidadorDatos.LeerPoblacion("17643060"));
        Assert.Equal(17643060L, ValidadorDatos.LeerPoblacion("17.643.060"));
        Assert.Equal(17643060L, ValidadorDatos.LeerPoblacion(" 17,643,060 "));
        Assert.Equal(0L, ValidadorDatos.LeerPoblacion("0"));
        Assert.Equal(-1L, ValidadorDatos.LeerPoblacion("12.5")); // no es entero
        Assert.Equal(-1L, ValidadorDatos.LeerPoblacion("abc"));
        Assert.Equal(-1L, ValidadorDatos.LeerPoblacion("-5"));
        Assert.Equal(-1L, ValidadorDatos.LeerPoblacion(""));
    }

    [Fact]
    public void LeeAreaConPuntoOComaDecimal()
    {
        Assert.Equal(276841.0, ValidadorDatos.LeerArea("276841"));
        Assert.Equal(2.5, ValidadorDatos.LeerArea("2,5"));
        Assert.Equal(2.5, ValidadorDatos.LeerArea("2.5"));
        Assert.Equal(0.0, ValidadorDatos.LeerArea(""));
        Assert.True(double.IsNaN(ValidadorDatos.LeerArea("-3")));
        Assert.True(double.IsNaN(ValidadorDatos.LeerArea("diez")));
    }
}
