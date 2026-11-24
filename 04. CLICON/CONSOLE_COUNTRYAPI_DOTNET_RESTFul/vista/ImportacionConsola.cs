using ec.edu.monster.controlador;
using ec.edu.monster.modelos;
using ec.edu.monster.servicios;

namespace ec.edu.monster.vista;

/// <summary>
/// «Importar países» (ECUD2): pide confirmación, importa todos los países de REST Countries a
/// través del servidor y muestra cuántos se recibieron, registraron y actualizaron.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ImportacionConsola : PantallaConsola, ImportacionVista
{
    private ApiControlador? controlador;

    public ImportacionConsola(Consola consola, SesionServicio sesion) : base(consola, sesion)
    {
    }

    public void EstablecerControlador(ApiControlador apiControlador)
    {
        controlador = apiControlador;
    }

    public void Mostrar()
    {
        consola.Titulo("Importar países");
        consola.Ayuda("Trae todos los países de la API REST Countries. Los nuevos se registran y los existentes "
            + "se actualizan, sin duplicar.");
        Ejecutar(controlador!.Importar);
    }

    public bool ConfirmarImportacion()
    {
        bool confirmado = consola.Confirmar(
            "¿Importar los países desde la API REST Countries? Los países existentes se actualizarán");
        if (confirmado)
        {
            consola.Ayuda("Procesando...");
        }
        return confirmado;
    }

    public void MostrarResumen(ResumenImportacion resumen)
    {
        consola.MostrarTabla(new Tabla("Recibidos de la API", "Registrados (nuevos)", "Actualizados")
            .Derecha(0, 1, 2)
            .Fila(Tabla.Entero(resumen.Recibidos), Tabla.Entero(resumen.Registrados), Tabla.Entero(resumen.Actualizados)));
        consola.Mensaje("Importación finalizada: " + resumen.Recibidos + " países recibidos, "
            + resumen.Registrados + " registrados y " + resumen.Actualizados + " actualizados");
    }
}
