using Microsoft.AspNetCore.Identity;              // Manejo de usuarios y roles
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Integración de Identity con EF Core
using Microsoft.EntityFrameworkCore;              // Acceso y configuración de la base de datos
using ProyectoCine.Models;                        // Modelos propios del proyecto

namespace ProyectoCine.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        // Definición de las tablas en la base de datos
        public DbSet<Cine> Cines { get; set; }
        public DbSet<Pelicula> Peliculas { get; set; }
        public DbSet<Sala> Salas { get; set; }
        public DbSet<Funcion> Funciones { get; set; }


    }
}