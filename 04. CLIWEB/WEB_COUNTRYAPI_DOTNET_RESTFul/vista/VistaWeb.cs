namespace ec.edu.monster.vista;

/// <summary>
/// Vista web (equivale al PanelBase de escritorio). Cada usuario tiene sus propias vistas, que
/// se enlazan con la petición en curso mientras el controlador trabaja: lo que el controlador
/// «muestra» queda como atributo de la petición (<c>HttpContext.Items</c>) y la vista Razor lo
/// dibuja después.
/// <list type="bullet">
///   <item><c>mensaje</c>: aviso verde;</item>
///   <item><c>error</c>: aviso rojo.</item>
/// </list>
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public abstract class VistaWeb : VistaBase
{
    public const string MENSAJE = "mensaje";
    public const string ERROR = "error";

    private HttpContext? peticion;

    /// <summary>Enlaza la vista con la petición que se está atendiendo (el formulario ya está leído).</summary>
    public virtual void Atender(HttpContext contexto)
    {
        peticion = contexto;
    }

    /// <summary>Suelta la petición al terminar.</summary>
    public void Terminar()
    {
        peticion = null;
    }

    protected HttpContext Peticion() => peticion!;

    /// <summary>
    /// Parámetro del formulario o de la dirección sin espacios a los lados, o null si no se envió
    /// (como getParameter de los servlets).
    /// </summary>
    protected string? Parametro(string nombre)
    {
        return ParametroCrudo(nombre)?.Trim();
    }

    /// <summary>Parámetro sin espacios, o null si no se envió o está vacío.</summary>
    protected string? ParametroConValor(string nombre)
    {
        string? valor = Parametro(nombre);
        return string.IsNullOrEmpty(valor) ? null : valor;
    }

    /// <summary>El valor tal como llegó (la clave no se recorta).</summary>
    protected string? ParametroCrudo(string nombre)
    {
        HttpRequest request = peticion!.Request;
        if (request.HasFormContentType && request.Form.TryGetValue(nombre, out var deFormulario))
        {
            return deFormulario.ToString();
        }
        return request.Query.TryGetValue(nombre, out var deDireccion) ? deDireccion.ToString() : null;
    }

    protected void Poner(string atributo, object? valor)
    {
        peticion!.Items[atributo] = valor;
    }

    public void MostrarMensaje(string? mensaje)
    {
        Poner(MENSAJE, mensaje);
    }

    public void MostrarError(string? mensaje)
    {
        Poner(ERROR, mensaje);
    }

    /// <summary>
    /// El controlador ya cerró la sesión: el controlador MVC lo detecta con <c>EstaActiva()</c>
    /// y redirige al inicio de sesión con el mensaje de error.
    /// </summary>
    public void IrAInicioSesion()
    {
        // Nada que hacer aquí: lo resuelve el controlador MVC al terminar la acción.
    }
}
