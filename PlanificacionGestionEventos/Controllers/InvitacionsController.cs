using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionGestionEventos.Data;
using PlanificacionGestionEventos.Models;

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
            var invitaciones = await _context.Invitaciones
                .Include(i => i.Evento)
                .Include(i => i.Usuario)
                .ToListAsync();

            return View(invitaciones);
        }

        // FORMULARIO CREAR
        public IActionResult Create()
        {
            ViewBag.Eventos = _context.Eventos.ToList();
            return View();
        }

        // GUARDAR INVITACION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Invitacion invitacion)
        {
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

            if (ModelState.IsValid)
            {
                _context.Update(invitacion);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(invitacion);
        }

        // ELIMINAR
        public async Task<IActionResult> Delete(int id)
        {
            var invitacion = await _context.Invitaciones
                .Include(i => i.Evento)
                .FirstOrDefaultAsync(m => m.InvitacionId == id);

            if (invitacion == null)
                return NotFound();

            return View(invitacion);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invitacion = await _context.Invitaciones.FindAsync(id);

            if (invitacion != null)
            {
                _context.Invitaciones.Remove(invitacion);
                await _context.SaveChangesAsync();
            }

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
                        where r.Nombre == "Invitado" && u.Email.Contains(email)
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