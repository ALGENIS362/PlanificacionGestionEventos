using Microsoft.EntityFrameworkCore;
using PlanificacionGestionEventos.Models;

namespace PlanificacionGestionEventos.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Evento> Eventos { get; set; }
        public DbSet<Invitacion> Invitaciones { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UsuarioRole> UsuariosRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UsuarioRole>()
                .HasKey(ur => new { ur.UsuarioId, ur.RoleId });

            modelBuilder.Entity<UsuarioRole>()
                .HasOne(ur => ur.Usuario)
                .WithMany(u => u.UsuariosRoles)
                .HasForeignKey(ur => ur.UsuarioId);

            modelBuilder.Entity<UsuarioRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UsuariosRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<Evento>()
                .HasOne(e => e.Organizador)
                .WithMany(u => u.EventosOrganizados)
                .HasForeignKey(e => e.OrganizadorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invitacion>()
                .HasOne(i => i.Evento)
                .WithMany(e => e.Invitaciones)
                .HasForeignKey(i => i.EventoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Invitacion>()
                .HasOne(i => i.Usuario)
                .WithMany(u => u.Invitaciones)
                .HasForeignKey(i => i.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}