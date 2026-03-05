using System.ComponentModel.DataAnnotations;

namespace PlanificacionGestionEventos.Models
{
    public class Invitacion
    {
        [Key]
        public int InvitacionId { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string? CorreoInvitado { get; set; }

        public EstadoRSVP Estado { get; set; }

        public int EventoId { get; set; }

        public Evento? Evento { get; set; }

        public int UsuarioId { get; set; }

        public Usuario? Usuario { get; set; }
    }
}