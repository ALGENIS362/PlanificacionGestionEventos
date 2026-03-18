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
            if (!(User.IsInRole("Admin") || (User.IsInRole("Organizador") && evento.OrganizadorId == currentUserId)))
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
            // Si el usuario es Admin, mostrar dropdown de organizadores.
            // Si es Organizador, asignaremos el OrganizadorId automáticamente y no mostraremos el dropdown.
            if (User.IsInRole("Admin"))
            {
                ViewData["OrganizadorId"] = new SelectList(_context.Usuarios, "UsuarioId", "Email");
            }
            else if (User.IsInRole("Organizador"))
            {
                var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdClaim, out var userId))
                {
                    ViewBag.CurrentOrganizadorId = userId;
                }
            }

            // provide estado options
            ViewData["Estados"] = Enum.GetNames(typeof(Models.EventoEstado)).ToList();
            return View();
        }

        // POST: Eventoes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventoId,Nombre,Fecha,Hora,Lugar,Descripcion,Categoria,MaximoInvitados,OrganizadorId,Estado")] Evento evento)
        {
            var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", System.StringComparison.OrdinalIgnoreCase);

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

                // Verificar que el Organizador existe para evitar violación FK
                var organizadorExists = await _context.Usuarios.AnyAsync(u => u.UsuarioId == evento.OrganizadorId);
                if (!organizadorExists)
                {
                    ModelState.AddModelError("OrganizadorId", "El organizador seleccionado no existe.");
                    ViewData["OrganizadorId"] = new SelectList(_context.Usuarios, "UsuarioId", "Email", evento.OrganizadorId);
                    ViewData["Estados"] = Enum.GetNames(typeof(Models.EventoEstado)).ToList();
                    return View(evento);
                }

                // Save uploaded files
                var files = Request.Form.Files;
                if (files != null && files.Count > 0)
                {
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
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

                _context.Add(evento);
                await _context.SaveChangesAsync();

                if (isAjax)
                {
                    // return rendered card partial so client can insert HTML with antiforgery tokens present
                    var organizador = await _context.Usuarios.FindAsync(evento.OrganizadorId);
                    // include Organizador navigation
                    evento.Organizador = organizador;
                    return PartialView("_EventoCardPartial", evento);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["OrganizadorId"] = new SelectList(_context.Usuarios, "UsuarioId", "Email", evento.OrganizadorId);
            ViewData["Estados"] = Enum.GetNames(typeof(Models.EventoEstado)).ToList();
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
        public async Task<IActionResult> Edit(int id, [Bind("EventoId,Nombre,Fecha,Hora,Lugar,Descripcion,Categoria,MaximoInvitados,OrganizadorId,Estado")] Evento evento)
        {
            if (id != evento.EventoId)
            {
                return NotFound();
            }

            // 🔐 VALIDACIÓN DE SEGURIDAD (VA AQUÍ)
            var eventoDb = await _context.Eventos.FindAsync(id);

            if (eventoDb == null)
                return NotFound();

            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out var userId);

            if (!(User.IsInRole("Admin") || eventoDb.OrganizadorId == userId))
            {
                return Unauthorized();
            }

            var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", System.StringComparison.OrdinalIgnoreCase);

            if (ModelState.IsValid)
            {
                // (tu código sigue igual aquí)
                // handle uploaded files first (if any)
                var files = Request.Form.Files;
                if (files != null && files.Count > 0)
                {
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);
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
                        evento.Images = string.IsNullOrWhiteSpace(evento.Images) ? string.Join(';', saved) : evento.Images + ";" + string.Join(';', saved);
                    }
                }

                try
                {
                    _context.Update(evento);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventoExists(evento.EventoId))
                    {
                        if (isAjax) return Json(new { success = false, message = "Evento no encontrado." });
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    // FK or DB error
                    if (isAjax)
                    {
                        return Json(new { success = false, message = dbEx.InnerException?.Message ?? dbEx.Message });
                    }
                    throw;
                }

                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        evento = new
                        {
                            id = evento.EventoId,
                            nombre = evento.Nombre,
                            fecha = evento.Fecha.ToString("yyyy-MM-dd"),
                            hora = evento.Hora,
                            lugar = evento.Lugar,
                            maximo = evento.MaximoInvitados,
                            descripcion = evento.Descripcion,
                            categoria = evento.Categoria,
                            estado = evento.Estado.ToString()
                        }
                    });
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["OrganizadorId"] = new SelectList(_context.Usuarios, "UsuarioId", "Email", evento.OrganizadorId);
            ViewData["Estados"] = Enum.GetNames(typeof(Models.EventoEstado)).ToList();

            if (isAjax)
            {
                // Return partial with validation messages
                return PartialView("_EditPartial", evento);
            }

            return View(evento);
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

            if (!(User.IsInRole("Admin") || evento.OrganizadorId == userId))
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
