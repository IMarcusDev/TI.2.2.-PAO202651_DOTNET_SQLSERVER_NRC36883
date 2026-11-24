namespace ec.edu.monster.vista;

/// <summary>
/// Menú principal: en la web cada opción es una tarjeta con su enlace (menu.cshtml), así que
/// solo queda el saludo al entrar.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class MenuVistaWeb : VistaWeb, MenuVista
{
    public void MostrarMenu(string? nombre)
    {
        MostrarMensaje("Bienvenido, " + nombre + ".");
    }

    /// <summary>Cada opción del menú es un enlace; no hay opción que leer.</summary>
    public int LeerOpcion() => -1;
}
