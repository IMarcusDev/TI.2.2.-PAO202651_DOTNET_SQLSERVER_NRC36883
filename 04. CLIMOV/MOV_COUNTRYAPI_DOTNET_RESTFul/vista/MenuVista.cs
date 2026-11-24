namespace ec.edu.monster.vista;

/// <summary>
/// Menú principal: Importar países, Consultar país en la API, Países, Buscar y filtrar,
/// Reportes, Universidades y Cerrar sesión.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface MenuVista : VistaBase
{
    /// <summary>Muestra el menú con el nombre del usuario de la sesión.</summary>
    void MostrarMenu(string? nombre);

    /// <summary>Opción elegida (la usa la consola; en las vistas gráficas cada opción es un botón).</summary>
    int LeerOpcion();
}
