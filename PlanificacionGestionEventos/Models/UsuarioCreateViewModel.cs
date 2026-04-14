using System.ComponentModel.DataAnnotations;

namespace PlanificacionGestionEventos.Models
{
    public class UsuarioCreateViewModel
    {
        [MaxLength(150)]
        [Display(Name = "Nombre completo")]
        public string? NombreCompleto { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        [Display(Name = "Correo electrónico")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [Display(Name = "Teléfono")]
        [RegularExpression(@"^(809|829|849)\d{7}$", ErrorMessage = "Debe ser un número válido de RD (809, 829, 849 + 7 dígitos)")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "El teléfono debe tener 10 dígitos")]
        public string? Telefono { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos {1} caracteres.")]
        [Display(Name = "Contraseña")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Rol")]
        public string? SelectedRole { get; set; }
    }
}
