using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Secuencia>().HasKey(s => s.Nombre);

        // Defensa en la base: ningun numero se repite y un presupuesto no puede
        // tener dos facturas.
        modelBuilder.Entity<Presupuesto>().HasIndex(p => p.Numero).IsUnique();
        modelBuilder.Entity<Factura>().HasIndex(f => f.Numero).IsUnique();
        modelBuilder.Entity<Factura>().HasIndex(f => f.PresupuestoId).IsUnique();

        // Por defecto EF borraria la factura en cascada junto con su presupuesto.
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
