using System.ComponentModel.DataAnnotations;

namespace PlanificacionGestionEventos.Models
{
    public class Evento
    {
        [Key]
        public int EventoId { get; set; }

        [Required]
        [MaxLength(150)]
        public string? Nombre { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public string? Hora { get; set; }

        [Required]
        [MaxLength(150)]
        public string? Lugar { get; set; }

        [MaxLength(500)]
        public string? Descripcion { get; set; }
        [Required]
        public string? Categoria { get; set; }
        public int MaximoInvitados { get; set; }

        public int OrganizadorId { get; set; }

        public Usuario? Organizador { get; set; }

        public ICollection<Invitacion> Invitaciones { get; set; } = new List<Invitacion>();
    }
}