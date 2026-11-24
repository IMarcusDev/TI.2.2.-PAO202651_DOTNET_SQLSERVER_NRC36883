using ec.edu.monster.controlador;
using ec.edu.monster.modelos;
using ec.edu.monster.servicios;

namespace ec.edu.monster.vista;

/// <summary>
/// «Consultar país en la API» (ECUD3): busca en REST Countries a través del servidor, muestra si
/// cada resultado ya está guardado y permite guardar uno por su número (N°).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ConsultaApiConsola : PantallaConsola, ConsultaApiVista
{
    private ApiControlador? controlador;
    private List<Pais>? resultados;
    private Pais? seleccion;

    public ConsultaApiConsola(Consola consola, SesionServicio sesion) : base(consola, sesion)
    {
    }

    public void EstablecerControlador(ApiControlador apiControlador)
    {
        controlador = apiControlador;
    }

    /// <summary>Pantalla de consulta: consulta, ofrece guardar y pregunta si se consulta otro país.</summary>
    public void Mostrar()
    {
        consola.Titulo("Consultar país en la API");
        consola.Ayuda("Consulta REST Countries por nombre (por ejemplo «ecuador») y guarde el país elegido.");
        do
        {
            resultados = null;
            Ejecutar(controlador!.Consultar);
            if (sesion.EstaActiva() && resultados is { Count: > 0 })
            {
                string numero = consola.Leer("N° del país a guardar (Enter para no guardar)");
                if (numero.Length > 0)
                {
                    seleccion = PorNumero(numero);
                    Ejecutar(controlador.Guardar);
                }
            }
        }
        while (sesion.EstaActiva() && consola.Confirmar("¿Consultar otro país?"));
    }

    private Pais? PorNumero(string texto)
    {
        return int.TryParse(texto, out int numero) && numero >= 1 && numero <= resultados!.Count
            ? resultados[numero - 1]
            : null;
    }

    // ------------------------------------------------------------------ ConsultaApiVista

    public string? LeerNombre()
    {
        return consola.Leer("Nombre del país");
    }

    public void MostrarResultados(List<Pais> paises)
    {
        resultados = paises;
        Tabla tabla = new Tabla("N°", "Nombre", "Capital", "Región", "Población", "Área (km²)", "Moneda", "¿Guardado?")
            .Derecha(0, 4, 5)
            .Maximo(1, 24).Maximo(2, 16).Maximo(6, 26);
        for (int i = 0; i < paises.Count; i++)
        {
            Pais pais = paises[i];
            tabla.Fila(i + 1, pais.Nombre, pais.Capital, pais.Region, Tabla.Entero(pais.Poblacion),
                Tabla.Decimal(pais.Area), pais.Moneda, pais.Guardado ? "Sí" : "No");
        }
        consola.MostrarTabla(tabla);
    }

    public Pais? LeerSeleccion() => seleccion;
}
