using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// «Consultar país en la API»: nombre, resultados y botón Guardar (ECUD3).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface ConsultaApiVista : VistaBase
{
    string? LeerNombre();

    void MostrarResultados(List<Pais> paises);

    /// <summary>País elegido en los resultados, o null si no hay ninguno.</summary>
    Pais? LeerSeleccion();
}
