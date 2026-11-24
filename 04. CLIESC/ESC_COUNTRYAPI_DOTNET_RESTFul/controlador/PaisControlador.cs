using ec.edu.monster.modelos;
using ec.edu.monster.servicios;
using ec.edu.monster.vista;

namespace ec.edu.monster.controlador;

/// <summary>
/// Gestión de países (ECUD4: listar, buscar, registrar, actualizar y eliminar) y «Buscar y
/// filtrar» (ECUD5).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class PaisControlador
{
    internal const string TABLA_VACIA = "No hay países registrados. Use «Importar países» para cargarlos";
    internal const string SIN_RESULTADOS = "No se encontraron países";
    internal const string SIN_RESULTADOS_CRITERIOS = "No se encontraron países con esos criterios";
    internal const string SELECCIONE_PAIS = "Seleccione un país de la tabla";

    private readonly PaisVista vista;
    private readonly BusquedaVista vistaBusqueda;
    private readonly PaisServicio servicio;
    private readonly SesionServicio sesion;

    public PaisControlador(PaisVista vista, BusquedaVista vistaBusqueda, PaisServicio servicio, SesionServicio sesion)
    {
        this.vista = vista;
        this.vistaBusqueda = vistaBusqueda;
        this.servicio = servicio;
        this.sesion = sesion;
        vistaBusqueda.MostrarFiltrosRegion(Pais.REGIONES);
    }

    /// <summary>Muestra todos los países ordenados por nombre.</summary>
    public async Task Listar()
    {
        RespuestaDTO<List<Pais>> respuesta = await servicio.Listar(sesion.ObtenerToken(), null);
        if (Fallo(respuesta, vista))
        {
            return;
        }
        vista.MostrarTabla(respuesta.Datos!);
        if (respuesta.Datos!.Count == 0)
        {
            vista.MostrarMensaje(TABLA_VACIA);
        }
    }

    /// <summary>Filtra la tabla por el texto del campo «Buscar» (coincidencia parcial en el nombre).</summary>
    public async Task Buscar()
    {
        string? texto = vista.LeerFiltro();
        RespuestaDTO<List<Pais>> respuesta = await servicio.Listar(sesion.ObtenerToken(), texto);
        if (Fallo(respuesta, vista))
        {
            return;
        }
        vista.MostrarTabla(respuesta.Datos!);
        if (respuesta.Datos!.Count == 0)
        {
            vista.MostrarMensaje(string.IsNullOrWhiteSpace(texto) ? TABLA_VACIA : SIN_RESULTADOS);
        }
    }

    /// <summary>Pide los datos de un país nuevo y lo registra.</summary>
    public Task Registrar()
    {
        return GuardarFormulario(null);
    }

    /// <summary>Edita el país elegido en la tabla.</summary>
    public Task Actualizar()
    {
        Pais? seleccionado = vista.PaisSeleccionado();
        if (seleccionado == null)
        {
            vista.MostrarError(SELECCIONE_PAIS);
            return Task.CompletedTask;
        }
        return GuardarFormulario(seleccionado.Copia());
    }

    /// <summary>Elimina el país elegido después de pedir confirmación.</summary>
    public async Task Eliminar()
    {
        Pais? seleccionado = vista.PaisSeleccionado();
        if (seleccionado == null)
        {
            vista.MostrarError(SELECCIONE_PAIS);
            return;
        }
        if (!vista.ConfirmarEliminacion(seleccionado))
        {
            return;
        }
        RespuestaDTO<object> respuesta = await servicio.Eliminar(sesion.ObtenerToken(), seleccionado.Id);
        if (Fallo(respuesta, vista))
        {
            if (respuesta.CodigoHttp == 404)
            {
                // Otro usuario ya lo eliminó: se recarga la tabla y el mensaje queda visible.
                await RecargarSinMensajes();
            }
            return;
        }
        await RecargarSinMensajes();
        vista.MostrarMensaje(respuesta.Mensaje);
    }

    /// <summary>Busca con los criterios de «Buscar y filtrar»; también se llama al cambiar la región.</summary>
    public async Task BuscarYFiltrar()
    {
        CriteriosBusqueda criterios = vistaBusqueda.LeerCriterios();
        RespuestaDTO<List<Pais>> respuesta = await servicio.Buscar(sesion.ObtenerToken(), criterios);
        if (Fallo(respuesta, vistaBusqueda))
        {
            return;
        }
        List<Pais> paises = respuesta.Datos!;
        vistaBusqueda.MostrarResultados(paises, paises.Count);
        if (paises.Count == 0)
        {
            vistaBusqueda.MostrarMensaje(SIN_RESULTADOS_CRITERIOS);
        }
    }

    /// <summary>
    /// Abre el formulario hasta que el usuario cancele o el servidor acepte los datos. Si el
    /// cliente o el servidor rechazan los datos (400 o 409), muestra el mensaje y vuelve a abrir
    /// el formulario con lo que el usuario había escrito.
    /// </summary>
    private async Task GuardarFormulario(Pais? inicial)
    {
        bool nuevo = inicial == null;
        int id = nuevo ? 0 : inicial!.Id;
        Pais? datos = inicial;
        while (true)
        {
            datos = vista.LeerPais(datos);
            if (datos == null)
            {
                return;
            }
            datos.Id = id;
            string? error = ValidadorDatos.ValidarPais(datos);
            if (error != null)
            {
                vista.MostrarError(error);
                continue;
            }
            RespuestaDTO<Pais> respuesta = nuevo
                ? await servicio.Registrar(sesion.ObtenerToken(), datos)
                : await servicio.Actualizar(sesion.ObtenerToken(), id, datos);
            if (!Fallo(respuesta, vista))
            {
                await RecargarSinMensajes();
                vista.MostrarMensaje(respuesta.Mensaje);
                return;
            }
            int codigo = respuesta.CodigoHttp;
            if (codigo != 400 && codigo != 409)
            {
                if (codigo == 404)
                {
                    await RecargarSinMensajes();
                }
                return;
            }
        }
    }

    /// <summary>Vuelve a cargar la tabla completa sin reemplazar el mensaje que se está mostrando.</summary>
    private async Task RecargarSinMensajes()
    {
        RespuestaDTO<List<Pais>> respuesta = await servicio.Listar(sesion.ObtenerToken(), null);
        if (respuesta.EsOk())
        {
            vista.MostrarTabla(respuesta.Datos!);
        }
    }

    /// <summary>
    /// Si la respuesta es un error lo muestra; si además la sesión expiró, la cierra y vuelve al
    /// inicio de sesión.
    /// </summary>
    /// <returns>true si hubo error</returns>
    private bool Fallo<T>(RespuestaDTO<T> respuesta, VistaBase vistaActual)
    {
        if (respuesta.EsOk())
        {
            return false;
        }
        vistaActual.MostrarError(respuesta.Mensaje);
        if (respuesta.SesionExpirada)
        {
            sesion.Cerrar();
            vistaActual.IrAInicioSesion();
        }
        return true;
    }
}
