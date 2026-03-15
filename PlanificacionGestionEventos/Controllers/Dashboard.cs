using Microsoft.AspNetCore.Mvc;
using PlanificacionGestionEventos.Data;
using PlanificacionGestionEventos.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;

namespace PlanificacionGestionEventos.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalUsuarios = await _context.Usuarios.CountAsync();
            var totalEventos = await _context.Eventos.CountAsync();
            var totalInvitaciones = await _context.Invitaciones.CountAsync();
            var confirmados = await _context.Invitaciones
                .CountAsync(i => i.Estado == EstadoRSVP.Confirmado);

            ViewBag.TotalUsuarios = totalUsuarios;
            ViewBag.TotalEventos = totalEventos;
            ViewBag.TotalInvitaciones = totalInvitaciones;
            ViewBag.Confirmados = confirmados;

            // Build dashboard model depending on role
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return View(new DashboardViewModel());
            }

            int userId = int.Parse(userIdClaim);

            if (User.IsInRole("Admin"))
            {
                var allEvents = await _context.Eventos.Include(e => e.Organizador).ToListAsync();
                var vm = new DashboardViewModel { Eventos = allEvents };
                return View(vm);
            }

            if (User.IsInRole("Organizador"))
            {
                var myEvents = await _context.Eventos
                    .Where(e => e.OrganizadorId == userId)
                    .Include(e => e.Invitaciones)
                    .ToListAsync();

                var vm = new DashboardViewModel { Eventos = myEvents };
                return View(vm);
            }

            var invitedEventIds = await _context.Invitaciones
                .Where(i => i.UsuarioId == userId)
                .Select(i => i.EventoId)
                .ToListAsync();

            var invitedEvents = await _context.Eventos
                .Where(e => invitedEventIds.Contains(e.EventoId))
                .Include(e => e.Organizador)
                .ToListAsync();

            var guestVm = new DashboardViewModel { Eventos = invitedEvents };
            return View(guestVm);
        }
    }
}
