using ec.edu.monster.controlador;
using ec.edu.monster.modelos;
using ec.edu.monster.servicios;

namespace ec.edu.monster.vista;

/// <summary>
/// Reportes (ECUD6): país más poblado, cantidad de países por región y promedio poblacional por
/// región.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ReporteConsola : PantallaConsola, ReporteVista
{
    private ReporteControlador? controlador;

    public ReporteConsola(Consola consola, SesionServicio sesion) : base(consola, sesion)
    {
    }

    public void EstablecerControlador(ReporteControlador reporteControlador)
    {
        controlador = reporteControlador;
    }

    public void Mostrar()
    {
        consola.Titulo("Reportes");
        consola.Ayuda("Estadísticas calculadas sobre los países almacenados.");
        Ejecutar(controlador!.Generar);
    }

    public void MostrarReporte(Reporte reporte)
    {
        consola.Subtitulo("País más poblado");
        Pais? pais = reporte.PaisMasPoblado;
        consola.Texto(pais == null ? "—"
            : pais.Nombre + " · " + Tabla.Entero(pais.Poblacion) + " habitantes · " + pais.Region);

        consola.Subtitulo("Cantidad de países por región");
        Tabla conteo = new Tabla("Región", "Países").Derecha(1);
        foreach (ConteoRegion fila in reporte.PaisesPorRegion)
        {
            conteo.Fila(fila.Region, Tabla.Entero(fila.Cantidad));
        }
        consola.MostrarTabla(conteo);

        consola.Subtitulo("Promedio poblacional por región");
        Tabla promedio = new Tabla("Región", "Promedio de población").Derecha(1);
        foreach (PromedioRegion fila in reporte.PromedioPorRegion)
        {
            promedio.Fila(fila.Region, Tabla.Entero(fila.PromedioPoblacion));
        }
        consola.MostrarTabla(promedio);
    }
}
