using System.Globalization;
using ec.edu.monster.controlador;
using ec.edu.monster.modelos;
using ec.edu.monster.servicios;

namespace ec.edu.monster.vista;

/// <summary>
/// Gestión de países (ECUD4): al entrar lista los países y ofrece buscar por nombre, registrar,
/// editar y eliminar. Para editar o eliminar se escribe el ID que aparece en la tabla.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class PaisConsola : PantallaConsola, PaisVista
{
    private PaisControlador? controlador;
    /// <summary>Última tabla mostrada: de aquí se toma el país por su ID.</summary>
    private List<Pais> ultimaTabla = [];

    public PaisConsola(Consola consola, SesionServicio sesion) : base(consola, sesion)
    {
    }

    public void EstablecerControlador(PaisControlador paisControlador)
    {
        controlador = paisControlador;
    }

    /// <summary>Pantalla «Países»; vuelve al menú con 0 o si la sesión expira.</summary>
    public void Mostrar()
    {
        consola.Titulo("Países");
        Ejecutar(controlador!.Listar);
        while (sesion.EstaActiva())
        {
            consola.LineaEnBlanco();
            consola.Texto("1. Listar todos   2. Buscar por nombre   3. Nuevo   4. Editar   5. Eliminar   0. Volver");
            switch (consola.LeerOpcion("Opción"))
            {
                case 1: Ejecutar(controlador.Listar); break;
                case 2: Ejecutar(controlador.Buscar); break;
                case 3: Ejecutar(controlador.Registrar); break;
                case 4: Ejecutar(controlador.Actualizar); break;
                case 5: Ejecutar(controlador.Eliminar); break;
                case 0: return;
                default: consola.Error(OPCION_NO_VALIDA); break;
            }
        }
    }

    /// <summary>Tabla de países que comparten Países y Buscar y filtrar (cabe en 120 columnas).</summary>
    internal static Tabla TablaPaises(List<Pais> paises)
    {
        Tabla tabla = new Tabla("ID", "Nombre", "Capital", "Región", "Subregión", "Población", "Área (km²)", "Moneda")
            .Derecha(0, 5, 6)
            .Maximo(1, 18).Maximo(2, 12).Maximo(3, 9).Maximo(4, 14).Maximo(7, 14);
        foreach (Pais pais in paises)
        {
            tabla.Fila(pais.Id, pais.Nombre, pais.Capital, pais.Region, pais.SubRegion,
                Tabla.Entero(pais.Poblacion), Tabla.Decimal(pais.Area), pais.Moneda);
        }
        return tabla;
    }

    internal static string TotalPaises(int cantidad)
    {
        return cantidad == 1 ? "1 país" : cantidad + " países";
    }

    // ------------------------------------------------------------------ PaisVista

    public void MostrarTabla(List<Pais> paises)
    {
        ultimaTabla = paises;
        if (paises.Count == 0)
        {
            return;
        }
        consola.MostrarTabla(TablaPaises(paises));
        consola.Ayuda(TotalPaises(paises.Count));
    }

    public string? LeerFiltro()
    {
        return consola.Leer("Nombre o parte del nombre");
    }

    /// <summary>
    /// Pide los datos campo por campo. Enter conserva el valor entre corchetes y «-» deja vacío
    /// un campo opcional. Devuelve null si el usuario no confirma.
    /// </summary>
    public Pais? LeerPais(Pais? inicial)
    {
        bool nuevo = inicial == null || inicial.Id == 0;
        consola.Subtitulo(nuevo ? "Nuevo país" : "Datos del país");
        consola.Ayuda("Enter conserva el valor entre corchetes; «-» deja vacío un campo opcional.");
        Pais origen = inicial ?? new Pais();
        string? nombre = Campo("Nombre *", origen.Nombre);
        string? capital = Campo("Capital", origen.Capital);
        string? region = LeerRegion(origen.Region);
        string? subregion = Campo("Subregión", origen.SubRegion);
        // Un valor inválido escrito antes (-1 / NaN) se vuelve a pedir sin valor por defecto.
        string? poblacion = Campo("Población *",
            inicial == null || origen.Poblacion < 0 ? null : origen.Poblacion.ToString(CultureInfo.InvariantCulture));
        string? area = Campo("Área (km²)",
            inicial == null || double.IsNaN(origen.Area) || origen.Area < 0 ? null : Editable(origen.Area));
        string? moneda = Campo("Moneda", origen.Moneda);
        if (!consola.Confirmar("¿Guardar los datos?"))
        {
            return null;
        }
        return new Pais(nombre ?? "", capital, region, subregion,
            ValidadorDatos.LeerPoblacion(poblacion ?? ""), ValidadorDatos.LeerArea(area), moneda);
    }

    public Pais? PaisSeleccionado()
    {
        string texto = consola.Leer("ID del país (columna ID de la tabla)");
        return int.TryParse(texto, out int id) ? ultimaTabla.FirstOrDefault(p => p.Id == id) : null;
    }

    public bool ConfirmarEliminacion(Pais pais)
    {
        return consola.Confirmar("¿Eliminar el país " + pais.Nombre + "?");
    }

    // ------------------------------------------------------------------ apoyo del formulario

    /// <summary>Lee un campo mostrando el valor actual; Enter lo conserva y «-» lo deja vacío.</summary>
    private string? Campo(string etiqueta, string? actual)
    {
        bool hayActual = !string.IsNullOrEmpty(actual);
        string texto = consola.Leer(hayActual ? etiqueta + " [" + actual + "]" : etiqueta);
        if (texto.Length == 0)
        {
            return hayActual ? actual : null;
        }
        return texto == "-" ? null : texto;
    }

    /// <summary>Región por número o por nombre; Enter conserva la actual. Una respuesta inválida queda vacía.</summary>
    private string? LeerRegion(string? actual)
    {
        string opciones = "Regiones:";
        for (int i = 0; i < Pais.REGIONES.Count; i++)
        {
            opciones += "  " + (i + 1) + ". " + Pais.REGIONES[i];
        }
        consola.Ayuda(opciones);
        string? elegida = Campo("Región *", actual);
        return elegida == null ? null : Region(elegida);
    }

    /// <summary>«2» o «europa» → «Europa»; cualquier otra cosa → null (ValidadorDatos pide elegir una).</summary>
    internal static string? Region(string texto)
    {
        if (int.TryParse(texto, out int numero))
        {
            return numero >= 1 && numero <= Pais.REGIONES.Count ? Pais.REGIONES[numero - 1] : null;
        }
        string buscada = texto.ToLowerInvariant();
        return Pais.REGIONES.FirstOrDefault(r => r.ToLowerInvariant() == buscada);
    }

    /// <summary>276841.5 → «276841,5»; 276841.0 → «276841».</summary>
    private static string Editable(double valor)
    {
        if (valor == Math.Round(valor))
        {
            return ((long)valor).ToString(CultureInfo.InvariantCulture);
        }
        return valor.ToString("R", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}
