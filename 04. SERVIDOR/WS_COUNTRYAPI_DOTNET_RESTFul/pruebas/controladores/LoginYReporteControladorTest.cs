using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.dto;
using ec.edu.monster.modelos.enums;
using ec.edu.monster.modelos.interfaces;
using ec.edu.monster.servicios;

namespace ec.edu.monster.controladores;

/// <summary>
/// Inicio de sesión (ECUD1) y reportes (ECUD6) con DAO en memoria.
/// </summary>
public class LoginYReporteControladorTest
{
    private sealed class UsuarioDAOFalso(Dictionary<string, Usuario> usuarios) : IUsuarioDAO
    {
        public Usuario? BuscarPorUsername(string username) => usuarios.GetValueOrDefault(username);
    }

    private readonly HashService hash = new();
    private readonly TokenService tokenService = new();
    private readonly LoginControlador login;

    public LoginYReporteControladorTest()
    {
        var usuarios = new Dictionary<string, Usuario>
        {
            ["monster"] = new(1, "monster", hash.Sha256("monster9"), "Marcos Escobar", EstadoUsuario.ACTIVO),
            ["baja"] = new(2, "baja", hash.Sha256("clave"), "Usuario Inactivo", EstadoUsuario.INACTIVO)
        };
        login = new LoginControlador(new UsuarioDAOFalso(usuarios), hash, tokenService);
    }

    [Fact]
    public void CredencialesCorrectasEntreganUnTokenValido()
    {
        Sesion sesion = login.Autenticar("monster", "monster9");
        Assert.NotNull(sesion.Token);
        Assert.Equal("Marcos Escobar", sesion.Usuario.Nombre);
        Assert.NotNull(tokenService.ValidarToken("Bearer " + sesion.Token));
    }

    [Fact]
    public void CualquierFallaDeCredencialesDaElMismoMensaje()
    {
        string?[][] casos = [["monster", "otra"], ["nadie", "monster9"], ["baja", "clave"], ["", ""], [null, null]];
        foreach (string?[] caso in casos)
        {
            var e = Assert.Throws<UnauthorizedAccessException>(() => login.Autenticar(caso[0], caso[1]));
            Assert.Equal("Credenciales incorrectas", e.Message);
        }
    }

    [Fact]
    public void ReportesSinPaisesEsNoEncontrado()
    {
        var reportes = new ReporteControlador(new DaosFalsos.PaisDAOFalso());
        var e = Assert.Throws<KeyNotFoundException>(() => reportes.GenerarReportes());
        Assert.Equal("No hay países registrados para generar reportes", e.Message);
    }

    [Fact]
    public void ReportesMuestranLasSeisRegionesYElMasPoblado()
    {
        var paisDAO = new DaosFalsos.PaisDAOFalso();
        paisDAO.Agregar("Ecuador", "América", 17_643_060);
        paisDAO.Agregar("China", "Asia", 1_402_112_000);
        ReporteDTO reporte = new ReporteControlador(paisDAO).GenerarReportes();
        Assert.Equal(6, reporte.PaisesPorRegion.Count);
        Assert.Equal("China", reporte.PaisMasPoblado!.Nombre);
    }
}
