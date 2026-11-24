namespace ec.edu.monster.vista;

/// <summary>Acceso desde código a la paleta de Resources/Styles/Estilos.xaml.</summary>
/// <remarks>Autor: Escobar Marcos</remarks>
internal static class Colores
{
    public static Color De(string nombre)
    {
        if (Application.Current != null && Application.Current.Resources.TryGetValue(nombre, out object? valor)
            && valor is Color color)
        {
            return color;
        }
        return Microsoft.Maui.Graphics.Colors.Black;
    }
}
