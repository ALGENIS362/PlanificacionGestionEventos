using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionGestionEventos.Data;
using PlanificacionGestionEventos.Models;

namespace PlanificacionGestionEventos.Controllers
{
    public class InvitacionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvitacionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var invitaciones = _context.Invitaciones
                .Include(i => i.Evento)
                .Include(i => i.Usuario);

            return View(await invitaciones.ToListAsync());
        }

        public IActionResult Create(int eventoId)
        {
            Invitacion invitacion = new Invitacion
            {
                EventoId = eventoId,
                Estado = EstadoRSVP.Pendiente
            };

            return View(invitacion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Invitacion invitacion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(invitacion);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Evento");
            }

            return View(invitacion);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var invitacion = await _context.Invitaciones.FindAsync(id);

            if (invitacion == null)
            {
                return NotFound();
            }

            return View(invitacion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Invitacion invitacion)
        {
            if (id != invitacion.InvitacionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(invitacion);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(invitacion);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var invitacion = await _context.Invitaciones
                .Include(i => i.Evento)
                .FirstOrDefaultAsync(m => m.InvitacionId == id);

            if (invitacion == null)
            {
                return NotFound();
            }

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
    }
}