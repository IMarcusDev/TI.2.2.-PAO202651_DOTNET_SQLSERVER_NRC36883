using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// Universidades: país en inglés, botón Importar y tabla de las guardadas (ECUD10).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface UniversidadVista : VistaBase
{
    /// <summary>País escrito (en inglés, como lo espera la API).</summary>
    string? LeerPais();

    void MostrarResumen(ResumenImportacion resumen);

    void MostrarUniversidades(List<Universidad> universidades);
}
