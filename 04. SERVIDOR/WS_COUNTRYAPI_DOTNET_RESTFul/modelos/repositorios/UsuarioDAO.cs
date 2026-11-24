using ec.edu.monster.config;
using ec.edu.monster.modelos.data;
using ec.edu.monster.modelos.interfaces;
using Microsoft.EntityFrameworkCore;

namespace ec.edu.monster.modelos.repositorios;

/// <summary>
/// DAO de la tabla Usuarios (EF Core; las consultas LINQ se envían parametrizadas).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class UsuarioDAO : IUsuarioDAO
{
    private readonly AppDbContext contexto;

    public UsuarioDAO(AppDbContext contexto)
    {
        this.contexto = contexto;
    }

    /// <summary>
    /// Busca por username sin distinguir mayúsculas ni tildes, como la intercalación de MySQL en TI.2.1.
    /// </summary>
    public Usuario? BuscarPorUsername(string username)
    {
        return contexto.Usuarios.AsNoTracking()
            .FirstOrDefault(u => EF.Functions.Collate(u.Username, AppDbContext.COLACION_SIN_TILDES) == username);
    }
}
