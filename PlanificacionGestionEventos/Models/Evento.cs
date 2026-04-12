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
        // Legacy single date/time kept for compatibility; prefer FechaInicio/FechaFin
        public DateTime Fecha { get; set; }

        public string? Hora { get; set; }

        // New: start and end datetimes for the event
        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

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

        [System.ComponentModel.DataAnnotations.Required]
        public EventoEstado Estado { get; set; } = EventoEstado.Activo;

        // Almacena rutas relativas de imágenes separadas por punto y coma ';'
        public string? Images { get; set; }

        public ICollection<Invitacion> Invitaciones { get; set; } = new List<Invitacion>();
    }
}