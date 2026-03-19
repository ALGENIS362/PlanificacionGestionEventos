using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlanificacionGestionEventos.Data;
using PlanificacionGestionEventos.Models;
using System.Security.Claims;

namespace PlanificacionGestionEventos.Controllers
{
    public class InvitacionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvitacionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTAR INVITACIONES
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            IQueryable<Invitacion> query = _context.Invitaciones
                .Include(i => i.Evento)
                .Include(i => i.Usuario);

            if (User.IsInRole("Participante"))
            {
                // 👤 SOLO VE SUS INVITACIONES
                query = query.Where(i => i.UsuarioId == userId);
            }
            else if (User.IsInRole("Organizador"))
            {
                // 🧑‍💼 SOLO INVITACIONES DE SUS EVENTOS
                query = query.Where(i => i.Evento != null && i.Evento.OrganizadorId == userId);
            }

            var invitaciones = await query.ToListAsync();

            return View(invitaciones);
        }

        // FORMULARIO CREAR
        public IActionResult Create()
        {
            ViewBag.EventoId = new SelectList(_context.Eventos, "EventoId", "Nombre");
            return View();
        }

        // GUARDAR INVITACION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Invitacion invitacion)
        {
            // 🔒 SOLO ORGANIZADOR
            if (!User.IsInRole("Organizador"))
                return Unauthorized();

            // 👤 OBTENER USUARIO LOGUEADO (SEGURO)
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            // 🔎 BUSCAR EVENTO
            var evento = await _context.Eventos.FindAsync(invitacion.EventoId);

            if (evento == null)
                return NotFound();

            // 🔒 VALIDAR QUE EL EVENTO ES DEL ORGANIZADOR
            if (evento.OrganizadorId != userId)
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                ViewBag.EventoId = new SelectList(_context.Eventos, "EventoId", "Nombre", invitacion.EventoId);
                return View(invitacion);
            }

            // ✅ GUARDAR
            if (ModelState.IsValid)
            {
                _context.Invitaciones.Add(invitacion);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(invitacion);
        }

        // EDITAR RSVP
        public async Task<IActionResult> Edit(int id)
        {
            var invitacion = await _context.Invitaciones.FindAsync(id);

            if (invitacion == null)
                return NotFound();

            return View(invitacion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Invitacion invitacion)
        {
            if (id != invitacion.InvitacionId)
                return NotFound();

            var invitacionDb = await _context.Invitaciones.FindAsync(id);

            if (invitacionDb == null)
                return NotFound();

            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out var userId);

            // 👤 SOLO EL PARTICIPANTE PUEDE RESPONDER
            if (User.IsInRole("Participante"))
            {
                if (invitacionDb.UsuarioId != userId)
                    return Unauthorized();

                // 👇 SOLO CAMBIA ESTADO (RSVP)
                invitacionDb.Estado = invitacion.Estado;
            }
            else
            {
                return Unauthorized();
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ELIMINAR
        public async Task<IActionResult> Delete(int id)
        {
            var invitacion = await _context.Invitaciones
                .Include(i => i.Evento)
                .FirstOrDefaultAsync(m => m.InvitacionId == id);

            if (invitacion == null)
                return NotFound();

            if (invitacion.Evento == null)
                return BadRequest("Evento inválido");

            // 👤 OBTENER USUARIO
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            // 🔐 VALIDAR QUE ES EL ORGANIZADOR DEL EVENTO
            if (!(User.IsInRole("Organizador") && invitacion.Evento.OrganizadorId == userId))
            {
                return Unauthorized();
            }

            return View(invitacion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invitacion = await _context.Invitaciones
                .Include(i => i.Evento)
                .FirstOrDefaultAsync(i => i.InvitacionId == id);

            if (invitacion == null)
                return NotFound();

            if (invitacion.Evento == null)
                return BadRequest();

            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            // 🔐 SOLO ORGANIZADOR DUEÑO DEL EVENTO
            if (!(User.IsInRole("Organizador") && invitacion.Evento.OrganizadorId == userId))
                return Unauthorized();

            _context.Invitaciones.Remove(invitacion);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Buscar usuarios con rol Invitado por email (JSON)
        [HttpGet]
        public async Task<IActionResult> SearchInvitados(string email, int eventoId)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new object[0]);

            var query = from u in _context.Usuarios
                        join ur in _context.UsuariosRoles on u.UsuarioId equals ur.UsuarioId
                        join r in _context.Roles on ur.RoleId equals r.RoleId
                        where r.Nombre == "Participante" && u.Email != null && u.Email.Contains(email)
                        select new
                        {
                            u.UsuarioId,
                            u.NombreCompleto,
                            u.Email,
                            u.Telefono
                        };

            var users = await query.Distinct().ToListAsync();
            return Json(users);
        }
    }
}