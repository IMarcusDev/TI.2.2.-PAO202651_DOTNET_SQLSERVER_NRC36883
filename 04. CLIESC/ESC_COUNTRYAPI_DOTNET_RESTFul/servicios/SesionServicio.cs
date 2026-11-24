namespace ec.edu.monster.servicios;

/// <summary>
/// Datos de la sesión abierta: el token que entrega el servidor y el nombre del usuario.
/// Solo vive en memoria mientras la aplicación está abierta.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class SesionServicio
{
    private volatile string? token;
    private volatile string? nombre;

    public void Guardar(string? token, string? nombre)
    {
        this.token = token;
        this.nombre = nombre;
    }

    public string? ObtenerToken() => token;

    public string? Nombre => nombre;

    public bool EstaActiva() => token != null;

    public void Cerrar()
    {
        token = null;
        nombre = null;
    }
}
