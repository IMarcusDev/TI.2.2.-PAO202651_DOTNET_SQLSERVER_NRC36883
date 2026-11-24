using ec.edu.monster.modelos.data;
using Microsoft.EntityFrameworkCore;

namespace ec.edu.monster.config;

/// <summary>
/// Sesión de Entity Framework Core con la base ApiPaisesDB de SQL Server. Mapea las cuatro tablas
/// de init.sql tal como están (no se usan migraciones: el esquema lo crea el script).
///
/// La cadena de conexión no está en el código: se lee de ConnectionStrings:ApiPaisesDB
/// (appsettings.json o la variable de entorno ConnectionStrings__ApiPaisesDB).
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Intercalación que no distingue mayúsculas ni tildes. MySQL (utf8mb4_0900_ai_ci) compara así en
    /// TI.2.1; la intercalación por defecto de SQL Server sí distingue tildes, así que las búsquedas e
    /// igualdades por texto la aplican de forma explícita (DUDAS.md, H3).
    /// </summary>
    public const string COLACION_SIN_TILDES = "Latin1_General_CI_AI";

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Region> Regiones => Set<Region>();
    public DbSet<Pais> Paises => Set<Pais>();
    public DbSet<Universidad> Universidades => Set<Universidad>();

    public AppDbContext(DbContextOptions<AppDbContext> opciones) : base(opciones)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasKey(u => u.IdUsuario);
            e.Property(u => u.Username).HasMaxLength(50);
            e.Property(u => u.PasswordHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
            e.Property(u => u.Nombre).HasMaxLength(100);
            // El estado se guarda como texto: 'ACTIVO' o 'INACTIVO'.
            e.Property(u => u.Estado).HasConversion<string>().HasMaxLength(10);
        });

        modelBuilder.Entity<Region>(e =>
        {
            e.ToTable("Regiones");
            e.HasKey(r => r.IdRegion);
            e.Property(r => r.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Pais>(e =>
        {
            e.ToTable("Paises");
            e.HasKey(p => p.Id);
            e.Property(p => p.Nombre).HasMaxLength(100);
            e.Property(p => p.Capital).HasMaxLength(100);
            e.Property(p => p.SubRegion).HasMaxLength(100);
            e.Property(p => p.Moneda).HasMaxLength(100);
            e.Property(p => p.FechaActualizacion).HasColumnType("datetime2(0)");
            e.HasOne(p => p.Region).WithMany().HasForeignKey(p => p.IdRegion);
        });

        modelBuilder.Entity<Universidad>(e =>
        {
            e.ToTable("Universidades");
            e.HasKey(u => u.Id);
            e.Property(u => u.Nombre).HasMaxLength(200);
            e.Property(u => u.Dominio).HasMaxLength(150);
            e.Property(u => u.SitioWeb).HasMaxLength(250);
            e.Property(u => u.Pais).HasMaxLength(100).IsRequired();
            e.Property(u => u.FechaActualizacion).HasColumnType("datetime2(0)");
        });
    }
}
