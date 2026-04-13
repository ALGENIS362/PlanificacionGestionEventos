using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlanificacionGestionEventos.Data;
using PlanificacionGestionEventos.Models;
using System.Security.Claims;

namespace PlanificacionGestionEventos.Controllers
{
    public class EventoesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventoesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: Eventoes/DeleteImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int eventoId, string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return Json(new { success = false, message = "Nombre de fichero inválido." });

            var evento = await _context.Eventos.FindAsync(eventoId);
            if (evento == null)
                return Json(new { success = false, message = "Evento no encontrado." });

            // autorización: solo Admin o propietario organizador
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out var currentUserId);
            if (!(User.IsInRole("Organizador") && evento.OrganizadorId == currentUserId))
            {
                return Json(new { success = false, message = "No autorizado." });
            }

            // Sanitize filename
            var safe = Path.GetFileName(filename);
            if (!string.Equals(safe, filename, StringComparison.Ordinal))
                return Json(new { success = false, message = "Nombre de fichero inválido." });

            if (string.IsNullOrWhiteSpace(evento.Images))
                return Json(new { success = false, message = "No hay imágenes." });

            var parts = evento.Images.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (!parts.Remove(filename))
                return Json(new { success = false, message = "Imagen no encontrada en el evento." });

            evento.Images = parts.Count > 0 ? string.Join(';', parts) : null;
            _context.Update(evento);
            await _context.SaveChangesAsync();

            // delete file from disk
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var filePath = Path.Combine(uploads, filename);
            try
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch
            {
                // ignore file delete errors
            }

            return Json(new { success = true });
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 6;

            ViewData["Estados"] = Enum.GetNames(typeof(Models.EventoEstado)).ToList();

            IQueryable<Evento> query = _context.Eventos
                .Include(e => e.Organizador);

            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out var userId);

            if (User.IsInRole("Organizador"))
            {
                query = query.Where(e => e.OrganizadorId == userId);
            }
            else if (User.IsInRole("Participante"))
            {
                query = _context.Invitaciones
                    .Where(i => i.UsuarioId == userId && i.Evento != null)
                    .Select(i => i.Evento!)
                    .Include(e => e.Organizador);
            }
            else
            {
                query = _context.Eventos.Where(e => false);
            }

            query = query.OrderByDescending(e => e.Fecha);

            var totalEventos = await query.CountAsync();

            var eventos = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalEventos / pageSize);

            return View(eventos);
        }

        // GET: Eventoes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await _context.Eventos
                .Include(e => e.Organizador)
                .FirstOrDefaultAsync(m => m.EventoId == id);
            if (evento == null)
            {
                return NotFound();
            }

            return View(evento);
        }

        // GET: Eventoes/Create
        public IActionResult Create()
        {
            if (!User.IsInRole("Organizador"))
                return Unauthorized();

            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            ViewBag.CurrentOrganizadorId = userId;

            ViewData["Estados"] = Enum.GetNames(typeof(Models.EventoEstado)).ToList();

            return View();
        }

        // POST: Eventoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventoId,Nombre,Fecha,Hora,FechaInicio,FechaFin,Lugar,Descripcion,Categoria,MaximoInvitados,OrganizadorId,Estado")] Evento evento)
        {
            var isAjax = string.Equals(
                Request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                System.StringComparison.OrdinalIgnoreCase
            );

            if (evento.FechaFin < evento.FechaInicio)
            {
                ModelState.AddModelError("", "La fecha fin no puede ser menor que la fecha inicio");
            }

            if (ModelState.IsValid)
            {
                // Si el usuario es Organizador, forzar el OrganizadorId al usuario actual
                if (User.IsInRole("Organizador"))
                {
                    var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                    if (int.TryParse(userIdClaim, out var userId))
                    {
                        evento.OrganizadorId = userId;
                    }
                }

                // Verificar que el Organizador existe
                var organizadorExists = await _context.Usuarios.AnyAsync(u => u.UsuarioId == evento.OrganizadorId);
                if (!organizadorExists)
                {
                    ModelState.AddModelError("OrganizadorId", "El organizador seleccionado no existe.");

                    ViewData["OrganizadorId"] = new SelectList(_context.Usuarios, "UsuarioId", "Email", evento.OrganizadorId);
                    ViewData["Estados"] = Enum.GetNames(typeof(Models.EventoEstado)).ToList();

                    if (User.IsInRole("Organizador"))
                    {
                        ViewBag.CurrentOrganizadorId = evento.OrganizadorId;
                    }

                    if (isAjax)
                        return PartialView("_CreatePartial", evento);

                    return View(evento);
                }

                // Validaciones: fechas
                if (evento.FechaInicio.Date < DateTime.Today)
                {
                    ModelState.AddModelError("FechaInicio", "La fecha debe ser hoy en adelante.");
                }

                if (evento.FechaFin <= evento.FechaInicio)
                {
                    ModelState.AddModelError("FechaFin", "La fecha/hora de fin debe ser posterior a la fecha/hora de inicio.");
                }

                // Comprobar solapamiento por lugar
                var overlapping = await _context.Eventos
                    .Where(e => e.Lugar == evento.Lugar &&
                                e.FechaInicio < evento.FechaFin &&
                                e.FechaFin > evento.FechaInicio)
                    .AnyAsync();

                if (overlapping)
                {
                    ModelState.AddModelError("Lugar", "Ya existe un evento en ese lugar y horario que se solapa.");
                }

                // Comprobar coincidencia exacta de lugar y hora (mismo lugar y mismo intervalo)
                var lugarNormalized = (evento.Lugar ?? string.Empty).Trim().ToLower();
                var exactMatch = await _context.Eventos
                    .Where(e => e.EventoId != evento.EventoId)
                    .Where(e => (e.Lugar ?? string.Empty).ToLower().Trim() == lugarNormalized)
                    .Where(e => e.FechaInicio == evento.FechaInicio && e.FechaFin == evento.FechaFin)
                    .AnyAsync();

                if (exactMatch)
                {
                    ModelState.AddModelError("Lugar", "Ya existe un evento exactamente en ese lugar y horario.");
                }

                // Si hay errores de validación, devolver la vista (o partial si es AJAX)
                if (!ModelState.IsValid)
                {
                    ViewData["OrganizadorId"] = new SelectList(_context.Usuarios, "UsuarioId", "Email", evento.OrganizadorId);
                    ViewData["Estados"] = Enum.GetNames(typeof(Models.EventoEstado)).ToList();
                    if (User.IsInRole("Organizador"))
                    {
                        ViewBag.CurrentOrganizadorId = evento.OrganizadorId;
                    }

                    if (isAjax)
                        return PartialView("_CreatePartial", evento);

                    return View(evento);
                }

                // Guardar imágenes
                var files = Request.Form.Files;
                if (files != null && files.Count > 0)
                {
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                    if (!Directory.Exists(uploads))
                        Directory.CreateDirectory(uploads);

                    var saved = new List<string>();

                    foreach (var file in files)
                    {
                        if (file.Length <= 0) continue;

                        var ext = Path.GetExtension(file.FileName);
                        var fileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploads, fileName);

                        using (var stream = System.IO.File.Create(filePath))
                        {
                            await file.CopyToAsync(stream);
                        }

                        saved.Add(fileName);
                    }

                    if (saved.Count > 0)
                    {
                        evento.Images = string.Join(';', saved);
                    }
                }

                // Mantener compatibilidad: rellenar Fecha y Hora con FechaInicio
                evento.Fecha = evento.FechaInicio.Date;
                evento.Hora = evento.FechaInicio.ToString("HH:mm");

                _context.Add(evento);
                await _context.SaveChangesAsync();

                if (isAjax)
                {
                    var organizador = await _context.Usuarios.FindAsync(evento.OrganizadorId);
                    evento.Organizador = organizador;
                    return PartialView("_EventoCardPartial", evento);
                }

                return RedirectToAction(nameof(Index));
            }

            // Si el modelo es inválido, volver a cargar combos
            ViewData["OrganizadorId"] = new SelectList(_context.Usuarios, "UsuarioId", "Email", evento.OrganizadorId);
            ViewData["Estados"] = Enum.GetNames(typeof(Models.EventoEstado)).ToList();

            if (User.IsInRole("Organizador"))
            {
                ViewBag.CurrentOrganizadorId = evento.OrganizadorId;
            }

            if (isAjax)
                return PartialView("_CreatePartial", evento);

            return View(evento);
        }

        // GET: Eventoes/EditModal/5
        public async Task<IActionResult> EditModal(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null)
            {
                return NotFound();
            }

            ViewData["Estados"] = Enum.GetNames(typeof(Models.EventoEstado)).ToList();
            return PartialView("_EditPartial", evento);
        }

        // GET: Eventoes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null)
            {
                return NotFound();
            }
            ViewData["OrganizadorId"] = new SelectList(_context.Usuarios, "UsuarioId", "Email", evento.OrganizadorId);
            return View(evento);
        }

        // POST: Eventoes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Evento evento)
        {
            if (id != evento.EventoId)
                return NotFound();

            // 🔎 Buscar el evento en BD (SOLO ESTE SE USA)
            var eventoDb = await _context.Eventos.FindAsync(id);

            if (eventoDb == null)
                return NotFound();

            // 🔐 VALIDAR USUARIO (SOLO ORGANIZADOR)
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            if (!(User.IsInRole("Organizador") && eventoDb.OrganizadorId == userId))
                return Unauthorized();

            // Validaciones de fechas
            if (evento.FechaInicio.Date < DateTime.Today)
            {
                ModelState.AddModelError("FechaInicio", "La fecha debe ser hoy en adelante.");
            }

            if (evento.FechaFin <= evento.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha/hora de fin debe ser posterior a la fecha/hora de inicio.");
            }

            // Comprobar solapamiento por lugar (excluyendo el propio evento)
            var overlapping = await _context.Eventos
                .Where(e => e.EventoId != id && e.Lugar == evento.Lugar &&
                            e.FechaInicio < evento.FechaFin &&
                            e.FechaFin > evento.FechaInicio)
                .AnyAsync();

            if (overlapping)
            {
                ModelState.AddModelError("Lugar", "Ya existe un evento en ese lugar y horario que se solapa.");
            }

            // Comprobar coincidencia exacta de lugar y hora (mismo lugar y mismo intervalo)
            var lugarNormalized = (evento.Lugar ?? string.Empty).Trim().ToLower();
            var exactMatch = await _context.Eventos
                .Where(e => e.EventoId != id)
                .Where(e => (e.Lugar ?? string.Empty).ToLower().Trim() == lugarNormalized)
                .Where(e => e.FechaInicio == evento.FechaInicio && e.FechaFin == evento.FechaFin)
                .AnyAsync();

            if (exactMatch)
            {
                ModelState.AddModelError("Lugar", "Ya existe un evento exactamente en ese lugar y horario.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // ✏ ACTUALIZAR CAMPOS (NO usar _context.Update)
            eventoDb.Nombre = evento.Nombre;
            eventoDb.Fecha = evento.FechaInicio.Date;
            eventoDb.Hora = evento.FechaInicio.ToString("HH:mm");
            eventoDb.FechaInicio = evento.FechaInicio;
            eventoDb.FechaFin = evento.FechaFin;
            eventoDb.Lugar = evento.Lugar;
            eventoDb.Descripcion = evento.Descripcion;
            eventoDb.Categoria = evento.Categoria;
            eventoDb.MaximoInvitados = evento.MaximoInvitados;
            eventoDb.Estado = evento.Estado;

            // 📸 MANEJO DE IMÁGENES
            var files = Request.Form.Files;

            if (files != null && files.Count > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);

                var nuevasImagenes = new List<string>();

                foreach (var file in files)
                {
                    if (file.Length <= 0) continue;

                    var ext = Path.GetExtension(file.FileName);
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploads, fileName);

                    using (var stream = System.IO.File.Create(filePath))
                    {
                        await file.CopyToAsync(stream);
                    }

                    nuevasImagenes.Add(fileName);
                }

                if (nuevasImagenes.Count > 0)
                {
                    if (string.IsNullOrWhiteSpace(eventoDb.Images))
                        eventoDb.Images = string.Join(';', nuevasImagenes);
                    else
                        eventoDb.Images += ";" + string.Join(';', nuevasImagenes);
                }
            }

            // 💾 GUARDAR
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // GET: Eventoes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await _context.Eventos
                .Include(e => e.Organizador)
                .FirstOrDefaultAsync(m => m.EventoId == id);
            if (evento == null)
            {
                return NotFound();
            }

            return View(evento);
        }

        // POST: Eventoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var evento = await _context.Eventos.FindAsync(id);

            if (evento == null)
                return NotFound();

            // 🔐 VALIDACIÓN DE SEGURIDAD (VA AQUÍ)
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out var userId);

            if (!(User.IsInRole("Organizador") && evento.OrganizadorId == userId))
            {
                return Unauthorized();
            }

            // 👇 SI PASA LA VALIDACIÓN, BORRA
            _context.Eventos.Remove(evento);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        private bool EventoExists(int id)
        {
            return _context.Eventos.Any(e => e.EventoId == id);
        }
    }
}
