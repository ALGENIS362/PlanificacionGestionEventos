using PlanificacionGestionEventos.Data;
using PlanificacionGestionEventos.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace PlanificacionGestionEventos.Services
{
    public interface IGuestUserService
    {
        Task<Usuario> GetOrCreateGuestUserAsync(ApplicationDbContext context);
    }

    public class GuestUserService : IGuestUserService
    {
        private const string GUEST_EMAIL = "guest.anonymous@system.local";
        private const string GUEST_NAME = "Invitado Anónimo";
        private const string GUEST_PHONE = "8095550000";

        public async Task<Usuario> GetOrCreateGuestUserAsync(ApplicationDbContext context)
        {
            var guestUser = await context.Usuarios.FirstOrDefaultAsync(u => u.Email == GUEST_EMAIL);

            if (guestUser == null)
            {
                guestUser = new Usuario
                {
                    Email = GUEST_EMAIL,
                    NombreCompleto = GUEST_NAME,
                    Telefono = GUEST_PHONE,
                    PasswordHash = HashPassword("SystemGuest@2024" + Guid.NewGuid().ToString())
                };

                context.Usuarios.Add(guestUser);
                await context.SaveChangesAsync();
            }

            return guestUser;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
