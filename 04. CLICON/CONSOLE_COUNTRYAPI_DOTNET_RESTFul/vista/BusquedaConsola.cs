using ec.edu.monster.controlador;
using ec.edu.monster.modelos;
using ec.edu.monster.servicios;

namespace ec.edu.monster.vista;

/// <summary>
/// «Buscar y filtrar» (ECUD5): nombre y capital parciales y una región (o todas). Cada búsqueda
/// consulta de nuevo al servidor.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class BusquedaConsola : PantallaConsola, BusquedaVista
{
    private PaisControlador? controlador;
    private List<string> regiones = [];

    public BusquedaConsola(Consola consola, SesionServicio sesion) : base(consola, sesion)
    {
    }

    public void EstablecerControlador(PaisControlador paisControlador)
    {
        controlador = paisControlador;
    }

    /// <summary>Pantalla «Buscar y filtrar»: busca hasta que el usuario no quiera otra búsqueda.</summary>
    public void Mostrar()
    {
        consola.Titulo("Buscar y filtrar");
        consola.Ayuda("Combine nombre, capital y región. Enter en un criterio significa «cualquiera».");
        do
        {
            Ejecutar(controlador!.BuscarYFiltrar);
        }
        while (sesion.EstaActiva() && consola.Confirmar("¿Hacer otra búsqueda?"));
    }

    // ------------------------------------------------------------------ BusquedaVista

    public CriteriosBusqueda LeerCriterios()
    {
        string nombre = consola.Leer("Nombre");
        string capital = consola.Leer("Capital");
        return new CriteriosBusqueda(VacioANulo(nombre), VacioANulo(capital), LeerRegion());
    }

    public void MostrarFiltrosRegion(IReadOnlyList<string> lista)
    {
        regiones = [.. lista];
    }

    public void MostrarResultados(List<Pais> paises, int total)
    {
        if (paises.Count > 0)
        {
            consola.MostrarTabla(PaisConsola.TablaPaises(paises));
        }
        consola.Subtitulo(total == 1 ? "1 país encontrado" : total + " países encontrados");
    }

    /// <summary>0 o Enter: todas las regiones; 1..n: una región. Repite la pregunta si el número no existe.</summary>
    private string? LeerRegion()
    {
        string opciones = "Región:  0. Todas";
        for (int i = 0; i < regiones.Count; i++)
        {
            opciones += "  " + (i + 1) + ". " + regiones[i];
        }
        while (true)
        {
            consola.Ayuda(opciones);
            string texto = consola.Leer("Región [0]");
            if (texto.Length == 0 || texto == "0")
            {
                return null;
            }
            if (int.TryParse(texto, out int numero) && numero >= 1 && numero <= regiones.Count)
            {
                return regiones[numero - 1];
            }
            consola.Error(OPCION_NO_VALIDA);
        }
    }

    private static string? VacioANulo(string? texto)
    {
        return string.IsNullOrEmpty(texto) ? null : texto;
    }
}
