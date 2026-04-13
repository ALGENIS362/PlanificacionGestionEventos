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
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return View(new DashboardViewModel());

            int userId = int.Parse(userIdClaim);

            int rechazados = 0;
            int totalEventos = 0;
            int totalInvitaciones = 0;
            int confirmados = 0;

            // 🔥 ADMIN VE TODO
            if (User.IsInRole("Admin"))
            {
                rechazados = await _context.Invitaciones
                    .CountAsync(i => i.Estado == EstadoRSVP.Rechazado);

                totalEventos = await _context.Eventos.CountAsync();

                totalInvitaciones = await _context.Invitaciones.CountAsync();

                confirmados = await _context.Invitaciones
                    .CountAsync(i => i.Estado == EstadoRSVP.Confirmado);

                var allEvents = await _context.Eventos
                    .Include(e => e.Organizador)
                    .ToListAsync();

                ViewBag.Rechazados = rechazados;
                ViewBag.TotalEventos = totalEventos;
                ViewBag.TotalInvitaciones = totalInvitaciones;
                ViewBag.Confirmados = confirmados;

                return View(new DashboardViewModel { Eventos = allEvents });
            }

            // 🔥 ORGANIZADOR SOLO SUS EVENTOS
            if (User.IsInRole("Organizador"))
            {
                var misEventos = await _context.Eventos
                    .Where(e => e.OrganizadorId == userId)
                    .ToListAsync();

                var misEventoIds = misEventos.Select(e => e.EventoId).ToList();

                totalEventos = misEventos.Count;

                totalInvitaciones = await _context.Invitaciones
                    .CountAsync(i => misEventoIds.Contains(i.EventoId));

                confirmados = await _context.Invitaciones
                    .CountAsync(i => misEventoIds.Contains(i.EventoId) && i.Estado == EstadoRSVP.Confirmado);

                rechazados = await _context.Invitaciones
                    .CountAsync(i => misEventoIds.Contains(i.EventoId) && i.Estado == EstadoRSVP.Rechazado);

                ViewBag.Rechazados = rechazados;
                ViewBag.TotalEventos = totalEventos;
                ViewBag.TotalInvitaciones = totalInvitaciones;
                ViewBag.Confirmados = confirmados;

                return View(new DashboardViewModel { Eventos = misEventos });
            }

            // 🔥 PARTICIPANTE SOLO SUS INVITACIONES
            var misInvitaciones = await _context.Invitaciones
                .Where(i => i.UsuarioId == userId)
                .ToListAsync();

            var eventoIds = misInvitaciones.Select(i => i.EventoId).ToList();

            var eventos = await _context.Eventos
                .Where(e => eventoIds.Contains(e.EventoId))
                .ToListAsync();

            totalEventos = eventos.Count;
            totalInvitaciones = misInvitaciones.Count;
            confirmados = misInvitaciones.Count(i => i.Estado == EstadoRSVP.Confirmado);
            rechazados = misInvitaciones.Count(i => i.Estado == EstadoRSVP.Rechazado);

            ViewBag.Rechazados = rechazados;
            ViewBag.TotalEventos = totalEventos;
            ViewBag.TotalInvitaciones = totalInvitaciones;
            ViewBag.Confirmados = confirmados;

            return View(new DashboardViewModel { Eventos = eventos });
        }
    }
}
