using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PlanificacionGestionEventos.Models
{
    public class Role
    {
        [DisplayName("ID del Rol")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [StringLength(50)]
        [DisplayName("Nombre del Rol")]
        public string? Nombre { get; set; }

        public ICollection<UsuarioRole> UsuariosRoles { get; set; } = new List<UsuarioRole>();
    }
}