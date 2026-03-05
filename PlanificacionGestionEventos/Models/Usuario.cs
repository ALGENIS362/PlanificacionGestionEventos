using System.ComponentModel.DataAnnotations;

namespace PlanificacionGestionEventos.Models
{
    public class Usuario
    {
    [Key]
    public int UsuarioId { get; set; }

    [MaxLength(150)]
    public string? NombreCompleto { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Telefono { get; set; }

    [Required]
    [MaxLength(500)]
    public string? PasswordHash { get; set; }

    public ICollection<Evento> EventosOrganizados { get; set; } = new List<Evento>();

    public ICollection<Invitacion> Invitaciones { get; set; } = new List<Invitacion>();

    public ICollection<UsuarioRole> UsuariosRoles { get; set; } = new List<UsuarioRole>();
}
}
