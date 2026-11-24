using ec.edu.monster.modelos;

namespace ec.edu.monster.vista;

/// <summary>
/// «Importar países»: confirmación y resumen (ECUD2).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public interface ImportacionVista : VistaBase
{
    /// <summary>«¿Importar los países desde la API REST Countries? Los países existentes se actualizarán».</summary>
    bool ConfirmarImportacion();

    void MostrarResumen(ResumenImportacion resumen);
}
