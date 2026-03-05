using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // GET: Invitacions
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Invitaciones.Include(i => i.Evento).Include(i => i.Usuario);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Invitacions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invitacion = await _context.Invitaciones
                .Include(i => i.Evento)
                .Include(i => i.Usuario)
                .FirstOrDefaultAsync(m => m.InvitacionId == id);
            if (invitacion == null)
            {
                return NotFound();
            }

            return View(invitacion);
        }

        // GET: Invitacions/Create
        public IActionResult Create()
        {
            ViewData["EventoId"] = new SelectList(_context.Eventos, "EventoId", "Hora");
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "Email");
            return View();
        }

        // POST: Invitacions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("InvitacionId,CorreoInvitado,Estado,EventoId,UsuarioId")] Invitacion invitacion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(invitacion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["EventoId"] = new SelectList(_context.Eventos, "EventoId", "Hora", invitacion.EventoId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "Email", invitacion.UsuarioId);
            return View(invitacion);
        }

        // GET: Invitacions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invitacion = await _context.Invitaciones.FindAsync(id);
            if (invitacion == null)
            {
                return NotFound();
            }
            ViewData["EventoId"] = new SelectList(_context.Eventos, "EventoId", "Hora", invitacion.EventoId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "Email", invitacion.UsuarioId);
            return View(invitacion);
        }

        // POST: Invitacions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("InvitacionId,CorreoInvitado,Estado,EventoId,UsuarioId")] Invitacion invitacion)
        {
            if (id != invitacion.InvitacionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(invitacion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InvitacionExists(invitacion.InvitacionId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["EventoId"] = new SelectList(_context.Eventos, "EventoId", "Hora", invitacion.EventoId);
            ViewData["UsuarioId"] = new SelectList(_context.Usuarios, "UsuarioId", "Email", invitacion.UsuarioId);
            return View(invitacion);
        }

        // GET: Invitacions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invitacion = await _context.Invitaciones
                .Include(i => i.Evento)
                .Include(i => i.Usuario)
                .FirstOrDefaultAsync(m => m.InvitacionId == id);
            if (invitacion == null)
            {
                return NotFound();
            }

            return View(invitacion);
        }

        // POST: Invitacions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invitacion = await _context.Invitaciones.FindAsync(id);
            if (invitacion != null)
            {
                _context.Invitaciones.Remove(invitacion);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InvitacionExists(int id)
        {
            return _context.Invitaciones.Any(e => e.InvitacionId == id);
        }
    }
}
