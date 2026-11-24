using ec.edu.monster.modelos;
using ec.edu.monster.vista;

namespace ec.edu.monster.dobles;

/// <summary>
/// Vista en memoria que implementa todas las interfaces de vista: guarda lo que el controlador
/// pidió mostrar y devuelve lo que la prueba programó como «entrada del usuario».
/// </summary>
public class VistaFalsa : LoginVista, MenuVista, PaisVista, BusquedaVista, ConsultaApiVista,
    ImportacionVista, ReporteVista, UniversidadVista
{
    // ---- lo que el controlador mostró
    public readonly List<string?> Mensajes = [];
    public readonly List<string?> Errores = [];
    public int VecesInicioSesion;
    public int VecesFormularioLogin;
    public bool ClaveLimpiada;
    public string? MenuMostradoPara;
    public List<Pais>? Tabla;
    public IReadOnlyList<string>? FiltrosRegion;
    public List<Pais>? ResultadosBusqueda;
    public int TotalBusqueda = -1;
    public List<Pais>? ResultadosConsulta;
    public ResumenImportacion? Resumen;
    public Reporte? Reporte;
    public List<Universidad>? Universidades;
    /// <summary>Datos con los que se abrió cada formulario de país.</summary>
    public readonly List<Pais?> FormulariosAbiertos = [];
    public Pais? ConfirmacionPedidaPara;

    // ---- lo que «escribe» el usuario
    public string Usuario = "";
    public string Clave = "";
    public string Filtro = "";
    /// <summary>Respuestas sucesivas del formulario de país; null significa «Cancelar».</summary>
    public readonly Queue<Pais?> RespuestasFormulario = new();
    public Pais? Seleccionado;
    public bool Confirmar = true;
    public CriteriosBusqueda Criterios = new();
    public string NombreConsulta = "";
    public Pais? SeleccionConsulta;
    public bool ConfirmarImportacionUsuario = true;
    public string PaisUniversidades = "";

    public string? UltimoError() => Errores.LastOrDefault();

    public string? UltimoMensaje() => Mensajes.LastOrDefault();

    public void MostrarMensaje(string? mensaje) => Mensajes.Add(mensaje);

    public void MostrarError(string? mensaje) => Errores.Add(mensaje);

    public void IrAInicioSesion() => VecesInicioSesion++;

    public void MostrarFormulario() => VecesFormularioLogin++;

    public LoginRequest LeerCredenciales() => new(Usuario, Clave);

    public void LimpiarClave() => ClaveLimpiada = true;

    public void MostrarMenu(string? nombre) => MenuMostradoPara = nombre;

    public int LeerOpcion() => 0;

    public void MostrarTabla(List<Pais> paises) => Tabla = paises;

    public string? LeerFiltro() => Filtro;

    public Pais? LeerPais(Pais? inicial)
    {
        FormulariosAbiertos.Add(inicial);
        return RespuestasFormulario.Count == 0 ? null : RespuestasFormulario.Dequeue();
    }

    public Pais? PaisSeleccionado() => Seleccionado;

    public bool ConfirmarEliminacion(Pais pais)
    {
        ConfirmacionPedidaPara = pais;
        return Confirmar;
    }

    public CriteriosBusqueda LeerCriterios() => Criterios;

    public void MostrarFiltrosRegion(IReadOnlyList<string> regiones) => FiltrosRegion = regiones;

    public void MostrarResultados(List<Pais> paises, int total)
    {
        ResultadosBusqueda = paises;
        TotalBusqueda = total;
    }

    public string? LeerNombre() => NombreConsulta;

    public void MostrarResultados(List<Pais> paises) => ResultadosConsulta = paises;

    public Pais? LeerSeleccion() => SeleccionConsulta;

    public bool ConfirmarImportacion() => ConfirmarImportacionUsuario;

    public void MostrarResumen(ResumenImportacion resumen) => Resumen = resumen;

    public void MostrarReporte(Reporte reporte) => Reporte = reporte;

    string? UniversidadVista.LeerPais() => PaisUniversidades;

    public void MostrarUniversidades(List<Universidad> universidades) => Universidades = universidades;
}
