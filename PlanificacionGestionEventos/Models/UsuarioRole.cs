namespace PlanificacionGestionEventos.Models
{
    public class UsuarioRole
    {
        public int UsuarioId { get; set; }
        public int RoleId { get; set; }
        public Usuario? Usuario { get; set; }
        public Role? Role { get; set; }
    }
}
