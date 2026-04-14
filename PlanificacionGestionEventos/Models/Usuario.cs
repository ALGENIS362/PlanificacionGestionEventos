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

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [Display(Name = "Teléfono")]
    [RegularExpression(@"^(809|829|849)\d{7}$", ErrorMessage = "Debe ser un número válido de RD (809, 829, 849 + 7 dígitos)")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "El teléfono debe tener 10 dígitos")]
    public string? Telefono { get; set; }

    [Required]
    [MaxLength(500)]
    public string? PasswordHash { get; set; }

    public ICollection<Evento> EventosOrganizados { get; set; } = new List<Evento>();

    public ICollection<Invitacion> Invitaciones { get; set; } = new List<Invitacion>();

    public ICollection<UsuarioRole> UsuariosRoles { get; set; } = new List<UsuarioRole>();
}
}
