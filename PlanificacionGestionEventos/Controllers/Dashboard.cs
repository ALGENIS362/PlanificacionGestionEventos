using Microsoft.AspNetCore.Mvc;
using PlanificacionGestionEventos.Data;
using PlanificacionGestionEventos.Models;

namespace PlanificacionGestionEventos.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var totalUsuarios = _context.Usuarios.Count();
            var totalEventos = _context.Eventos.Count();
            var totalInvitaciones = _context.Invitaciones.Count();
            var confirmados = _context.Invitaciones
                .Count(i => i.Estado == EstadoRSVP.Confirmado);

            ViewBag.TotalUsuarios = totalUsuarios;
            ViewBag.TotalEventos = totalEventos;
            ViewBag.TotalInvitaciones = totalInvitaciones;
            ViewBag.Confirmados = confirmados;

            return View();
        }
    }
}
