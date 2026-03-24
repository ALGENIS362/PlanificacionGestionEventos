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

            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            // 🔥 PRUEBA SIN FILTRO
            var invitaciones = await _context.Invitaciones
                .Include(i => i.Evento)
                .Include(i => i.Usuario)
                .ToListAsync();

            return View(invitaciones);
        }

        //GET FORMULARIO CREAR
        public IActionResult Create()
        {
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            // 🔥 SOLO EVENTOS DEL ORGANIZADOR LOGUEADO
            var eventos = _context.Eventos
                .Where(e => e.OrganizadorId == userId)
                .ToList();

            // 🔍 DEBUG (opcional)
            Console.WriteLine("Eventos encontrados: " + eventos.Count);

            ViewBag.EventoId = new SelectList(eventos, "EventoId", "Nombre");

            return View();
        }

        //POST GUARDAR INVITACION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Invitacion invitacion)
        {
            if (!User.IsInRole("Organizador"))
                return Unauthorized();

            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            // 🔎 BUSCAR EVENTO
            var evento = await _context.Eventos.FindAsync(invitacion.EventoId);

            // ✅ VALIDAR EVENTO (FALTABA ESTO)
            if (evento == null)
                return NotFound();

            if (evento.OrganizadorId != userId)
                return Unauthorized();

            // ✅ VALIDAR CORREO
            if (string.IsNullOrWhiteSpace(invitacion.CorreoInvitado))
            {
                ModelState.AddModelError("CorreoInvitado", "El correo es obligatorio.");

                ViewBag.EventoId = new SelectList(
                    _context.Eventos.Where(e => e.OrganizadorId == userId),
                    "EventoId",
                    "Nombre",
                    invitacion.EventoId
                );

                return View(invitacion);
            }

            // ✅ NORMALIZAR
            var correo = invitacion.CorreoInvitado.Trim().ToLower();

            // ✅ BUSCAR USUARIO
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.Email != null &&
                    u.Email.ToLower().Trim() == correo
                );

            // ✅ VALIDAR USUARIO
            if (usuario == null)
            {
                ModelState.AddModelError("CorreoInvitado", "Ese correo no existe.");

                ViewBag.EventoId = new SelectList(
                    _context.Eventos.Where(e => e.OrganizadorId == userId),
                    "EventoId",
                    "Nombre",
                    invitacion.EventoId
                );

                return View(invitacion);
            }

            // ✅ ASIGNAR USUARIO
            invitacion.UsuarioId = usuario.UsuarioId;

            // ✅ GUARDAR
            _context.Invitaciones.Add(invitacion);
            await _context.SaveChangesAsync();

            // 🔥 PRUEBA
            Console.WriteLine("INVITACION GUARDADA");
            return Content("Se guardó correctamente");
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

            if (User.IsInRole("Participante"))
            {
                if (invitacionDb.UsuarioId != userId)
                    return Unauthorized();

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

            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();
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

            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            if (!(User.IsInRole("Organizador") && invitacion.Evento.OrganizadorId == userId))
                return Unauthorized();

            _context.Invitaciones.Remove(invitacion);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}