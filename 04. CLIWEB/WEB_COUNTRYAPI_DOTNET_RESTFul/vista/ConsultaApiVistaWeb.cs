using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// «Consultar país en la API» (ECUD3) en consultar.cshtml. Los resultados quedan guardados para
/// este usuario: así «Guardar» y la recarga de la página no vuelven a gastar una consulta a
/// REST Countries.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class ConsultaApiVistaWeb : VistaWeb, ConsultaApiVista
{
    public const string RESULTADOS = "resultados";

    public List<Pais> Resultados { get; private set; } = [];

    public string UltimoNombre { get; private set; } = "";

    public string? LeerNombre()
    {
        string? nombre = Parametro("nombre");
        UltimoNombre = nombre ?? "";
        Resultados = [];
        return nombre;
    }

    public void MostrarResultados(List<Pais> paises)
    {
        Resultados = paises;
        Poner(RESULTADOS, paises);
    }

    /// <summary>El N° (desde 1) de la fila elegida, enviado por el botón «Guardar» de esa fila.</summary>
    public Pais? LeerSeleccion()
    {
        return int.TryParse(Parametro("numero"), out int numero) && numero >= 1 && numero <= Resultados.Count
            ? Resultados[numero - 1]
            : null;
    }
}
