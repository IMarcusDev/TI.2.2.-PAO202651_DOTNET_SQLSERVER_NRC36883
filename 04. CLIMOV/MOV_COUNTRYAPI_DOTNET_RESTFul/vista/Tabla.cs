namespace ec.edu.monster.vista;

/// <summary>
/// Tabla pequeña de dos columnas con encabezado celeste (reportes). La segunda columna es
/// numérica y va alineada a la derecha.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
internal static class Tabla
{
    public static void Llenar(VerticalStackLayout tabla, string columna1, string columna2, List<(string, string)> filas)
    {
        tabla.Clear();
        Grid encabezado = Fila(columna1, columna2, true);
        encabezado.BackgroundColor = Colores.De("AzulClaro");
        tabla.Add(encabezado);
        for (int i = 0; i < filas.Count; i++)
        {
            if (i > 0)
            {
                tabla.Add(new BoxView { HeightRequest = 1, Color = Colores.De("Borde") });
            }
            tabla.Add(Fila(filas[i].Item1, filas[i].Item2, false));
        }
    }

    private static Grid Fila(string texto1, string texto2, bool esEncabezado)
    {
        var fila = new Grid { ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)] };
        fila.Add(Celda(texto1, esEncabezado, TextAlignment.Start), 0);
        fila.Add(Celda(texto2, esEncabezado, TextAlignment.End), 1);
        return fila;
    }

    private static Label Celda(string texto, bool esEncabezado, TextAlignment alineacion)
    {
        return new Label
        {
            Text = texto,
            Padding = new Thickness(10),
            HorizontalTextAlignment = alineacion,
            FontSize = esEncabezado ? 13 : 14,
            FontFamily = esEncabezado ? "sans-serif-medium" : null,
            TextColor = Colores.De(esEncabezado ? "Azul" : "Texto")
        };
    }
}
