using ec.edu.monster.controlador;
using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Gestión de países (ECUD4) en paises.cshtml.
///
/// Cómo encaja el ciclo del controlador (pedir datos, validar, reabrir el formulario si hay
/// error) con una petición web: la primera llamada a <see cref="LeerPais"/> devuelve lo que el
/// usuario envió; si el controlador la vuelve a llamar es porque hubo un error, y entonces la
/// vista deja el formulario pendiente (con lo escrito) y devuelve null, como si el usuario
/// hubiera cerrado el diálogo. paises.cshtml lo muestra abierto con el mensaje.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class PaisVistaWeb : VistaWeb, PaisVista
{
    /// <summary>Lista de países de la tabla.</summary>
    public const string PAISES = "paises";
    /// <summary>País del formulario abierto (nuevo, en edición o devuelto con error).</summary>
    public const string FORMULARIO = "formulario";
    /// <summary>País que se va a eliminar (panel de confirmación).</summary>
    public const string ELIMINAR = "eliminar";

    private bool datosLeidos;

    /// <summary>Última tabla mostrada a este usuario: de aquí sale el país elegido por su ID.</summary>
    public List<Pais> UltimaTabla { get; private set; } = [];

    public override void Atender(HttpContext contexto)
    {
        base.Atender(contexto);
        datosLeidos = false;
    }

    /// <summary>País de la última tabla con ese ID, o null.</summary>
    public Pais? Buscar(string? id)
    {
        return int.TryParse(id, out int numero) ? UltimaTabla.FirstOrDefault(p => p.Id == numero) : null;
    }

    // ------------------------------------------------------------------ PaisVista

    public void MostrarTabla(List<Pais> paises)
    {
        UltimaTabla = paises;
        Poner(PAISES, paises);
    }

    public string? LeerFiltro()
    {
        return Parametro("q");
    }

    public Pais? LeerPais(Pais? inicial)
    {
        if (!datosLeidos)
        {
            datosLeidos = true;
            return LeerFormulario();
        }
        // Segunda llamada: el controlador reabre el formulario por un error de validación,
        // 400 o 409. Se devuelve a la página con lo que el usuario escribió.
        Poner(FORMULARIO, inicial);
        return null;
    }

    public Pais? PaisSeleccionado()
    {
        return Buscar(Parametro("id"));
    }

    public bool ConfirmarEliminacion(Pais pais)
    {
        return Parametro("confirmado") == "si";
    }

    /// <summary>Arma el país con los campos del formulario, como FormularioPaisDialogo en escritorio.</summary>
    private Pais LeerFormulario()
    {
        return new Pais(
            Parametro("nombre") ?? "",
            ParametroConValor("capital"),
            ParametroConValor("region"),
            ParametroConValor("subregion"),
            ValidadorDatos.LeerPoblacion(Parametro("poblacion") ?? ""),
            ValidadorDatos.LeerArea(Parametro("area")),
            ParametroConValor("moneda"));
    }
}
