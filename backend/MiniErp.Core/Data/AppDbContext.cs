using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MiniErp.Core.Models;

namespace MiniErp.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Presupuesto> Presupuestos => Set<Presupuesto>();
    public DbSet<PresupuestoItem> PresupuestoItems => Set<PresupuestoItem>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<Secuencia> Secuencias => Set<Secuencia>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        CompletarVencimientos();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        CompletarVencimientos();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Ningun presupuesto nuevo se guarda sin vencimiento, aunque quien lo arme solo
    // indique ValidezDias.
    private void CompletarVencimientos()
    {
        foreach (var entrada in ChangeTracker.Entries<Presupuesto>())
        {
            var presupuesto = entrada.Entity;
            if (entrada.State == EntityState.Added && presupuesto.FechaVencimiento == default)
                presupuesto.FechaVencimiento =
                    ZonaArgentina.DiaDe(presupuesto.Fecha).AddDays(presupuesto.ValidezDias);
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // EF avisa porque Factura y PresupuestoItem requieren un Presupuesto que el
        // filtro de baja logica puede ocultar. Aca no pasa: la base impide eliminar un
        // facturado y los items solo se consultan a traves de su presupuesto. Si se
        // agrega otra relacion con Presupuesto, hay que volver a revisar este punto.
        optionsBuilder.ConfigureWarnings(avisos => avisos.Ignore(
            CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Secuencia>().HasKey(s => s.Nombre);

        // Defensa en la base: ningun numero se repite y un presupuesto no puede
        // tener dos facturas.
        modelBuilder.Entity<Presupuesto>().HasIndex(p => p.Numero).IsUnique();
        modelBuilder.Entity<Factura>().HasIndex(f => f.Numero).IsUnique();
        modelBuilder.Entity<Factura>().HasIndex(f => f.PresupuestoId).IsUnique();

        // Baja logica: los eliminados no aparecen en ninguna consulta, salvo que se
        // pida con IgnoreQueryFilters().
        modelBuilder.Entity<Presupuesto>().HasQueryFilter(p => p.EliminadoEn == null);

        // Defensa en la base: un presupuesto facturado no se puede eliminar. La FK no
        // alcanza, porque una baja logica es un UPDATE.
        modelBuilder.Entity<Presupuesto>().ToTable(tabla => tabla.HasCheckConstraint(
            "CK_Presupuestos_FacturadoNoSeElimina",
            $"EliminadoEn IS NULL OR Estado <> {(int)EstadoPresupuesto.Facturado}"));

        // Ante un borrado fisico, EF borraria por defecto la factura en cascada.
        modelBuilder.Entity<Factura>()
            .HasOne(f => f.Presupuesto)
            .WithMany()
            .HasForeignKey(f => f.PresupuestoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Articulo>().Property(a => a.PrecioUnitario).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Articulo>().Property(a => a.AlicuotaIva).HasColumnType("decimal(5,2)");

        modelBuilder.Entity<PresupuestoItem>().Property(i => i.PrecioUnitario).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<PresupuestoItem>().Property(i => i.DescuentoPct).HasColumnType("decimal(5,2)");
        modelBuilder.Entity<PresupuestoItem>().Property(i => i.AlicuotaIva).HasColumnType("decimal(5,2)");

        modelBuilder.Entity<Factura>().Property(f => f.Subtotal).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Factura>().Property(f => f.Iva).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Factura>().Property(f => f.Total).HasColumnType("decimal(18,2)");
    }
}
