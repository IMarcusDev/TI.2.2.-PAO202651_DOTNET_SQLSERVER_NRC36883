using ec.edu.monster.controlador;
using ec.edu.monster.modelos;
using ec.edu.monster.servicios;

namespace ec.edu.monster.vista;

/// <summary>
/// Universidades (ECUD10): importa desde la API de universidades las de un país (en inglés) y
/// muestra las guardadas. El país propuesto es el último escrito («Ecuador» al inicio).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class UniversidadConsola : PantallaConsola, UniversidadVista
{
    private UniversidadControlador? controlador;
    private string ultimoPais = "Ecuador";

    public UniversidadConsola(Consola consola, SesionServicio sesion) : base(consola, sesion)
    {
    }

    public void EstablecerControlador(UniversidadControlador universidadControlador)
    {
        controlador = universidadControlador;
    }

    /// <summary>Pantalla «Universidades»; vuelve al menú con 0 o si la sesión expira.</summary>
    public void Mostrar()
    {
        consola.Titulo("Universidades");
        consola.Ayuda("Importa desde la API de universidades las de un país (escrito en inglés) y consulta las guardadas.");
        while (sesion.EstaActiva())
        {
            consola.LineaEnBlanco();
            consola.Texto("1. Importar   2. Ver guardadas   0. Volver");
            switch (consola.LeerOpcion("Opción"))
            {
                case 1: Ejecutar(controlador!.Importar); break;
                case 2: Ejecutar(controlador!.Listar); break;
                case 0: return;
                default: consola.Error(OPCION_NO_VALIDA); break;
            }
        }
    }

    // ------------------------------------------------------------------ UniversidadVista

    public string? LeerPais()
    {
        string texto = consola.Leer("País en inglés [" + ultimoPais + "]");
        if (texto.Length > 0)
        {
            ultimoPais = texto;
        }
        return ultimoPais;
    }

    public void MostrarResumen(ResumenImportacion resumen)
    {
        consola.Mensaje("Importación de universidades finalizada: " + resumen.Recibidos + " recibidas, "
            + resumen.Registrados + " registradas y " + resumen.Actualizados + " actualizadas");
    }

    public void MostrarUniversidades(List<Universidad> universidades)
    {
        if (universidades.Count > 0)
        {
            Tabla tabla = new Tabla("Nombre", "Dominio", "Sitio web").Maximo(0, 50).Maximo(1, 24).Maximo(2, 38);
            foreach (Universidad universidad in universidades)
            {
                tabla.Fila(universidad.Nombre, universidad.Dominio, universidad.SitioWeb);
            }
            consola.MostrarTabla(tabla);
        }
        consola.Ayuda(universidades.Count == 1 ? "1 universidad" : universidades.Count + " universidades");
    }
}
