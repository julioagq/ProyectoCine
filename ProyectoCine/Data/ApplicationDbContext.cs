using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProyectoCine.Models;

namespace ProyectoCine.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cine> Cines { get; set; }
        public DbSet<Pelicula> Peliculas { get; set; }
        public DbSet<Sala> Salas { get; set; }
        public DbSet<Funcion> Funciones { get; set; }
        public DbSet<CarritoItem> CarritoItems { get; set; }
        public DbSet<Orden> Ordenes { get; set; }
        public DbSet<DetalleOrden> DetalleOrdenes { get; set; }
        public DbSet<ReservaButaca> ReservasButacas { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ReservaButaca>()
                .HasIndex(r => new { r.FuncionId, r.ButacaCodigo })
                .IsUnique();

            builder.Entity<CarritoItem>()
                .Property(c => c.Precio)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Orden>()
                .Property(o => o.TotalGeneral)
                .HasColumnType("decimal(18,2)");

            builder.Entity<DetalleOrden>()
                .Property(d => d.PrecioUnitario)
                .HasColumnType("decimal(18,2)");

            builder.Entity<DetalleOrden>()
                .Property(d => d.SubTotal)
                .HasColumnType("decimal(18,2)");
        }
    }
}
