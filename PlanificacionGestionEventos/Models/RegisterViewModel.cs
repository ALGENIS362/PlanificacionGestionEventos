using System.ComponentModel.DataAnnotations;

namespace PlanificacionGestionEventos.Models
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Nombre")]
        [MaxLength(150)]
        public string Name { get; set; } = "";

        [Required]
        [EmailAddress]
        [Display(Name = "Correo")]
        public string Email { get; set; } = "";

        [Display(Name = "Teléfono")]
        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono debe tener exactamente 10 dígitos.")]
        public string? Telefono { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos {1} caracteres.")]
        public string Password { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string? ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Registrarse como")]
        public string Role { get; set; } = "";
    }
}
