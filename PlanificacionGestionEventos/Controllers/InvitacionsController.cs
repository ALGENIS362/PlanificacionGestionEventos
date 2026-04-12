using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PlanificacionGestionEventos.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using PlanificacionGestionEventos.Models;
using System.Security.Claims;
using System.Text;

namespace PlanificacionGestionEventos.Controllers
{
    public class InvitacionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.DataProtection.IDataProtector _protector;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public InvitacionsController(ApplicationDbContext context, Microsoft.AspNetCore.DataProtection.IDataProtectionProvider protectorProvider, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _context = context;
            _protector = protectorProvider.CreateProtector("InvitacionProtector-v1");
            _config = config;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Accept(string t)
        {
            if (string.IsNullOrEmpty(t))
                return BadRequest();

            string protectedBase64 = System.Net.WebUtility.UrlDecode(t);
            string json;

            try
            {
                var protectedBytes = Convert.FromBase64String(protectedBase64);
                var unprotectedBytes = _protector.Unprotect(protectedBytes);
                json = Encoding.UTF8.GetString(unprotectedBytes);
            }
            catch
            {
                return BadRequest("Token inválido o expirado.");
            }

            var doc = System.Text.Json.JsonDocument.Parse(json);
            var invitacionId = doc.RootElement.GetProperty("InvitacionId").GetInt32();
            var correo = doc.RootElement.GetProperty("Correo").GetString();

            var invitacion = await _context.Invitaciones.FindAsync(invitacionId);

            if (invitacion == null)
                return NotFound();

            // 🔒 evitar doble acción
            if (invitacion.Estado == EstadoRSVP.Confirmado)
                return View("Aceptado");

            if (invitacion.Estado == EstadoRSVP.Rechazado)
                return View("Rechazado");

            // ✅ SI ESTÁ LOGUEADO
            if (User?.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdClaim, out int currentUserId))
                    return Unauthorized();

                invitacion.UsuarioId = currentUserId;

                // 🔥 CORRECTO
                invitacion.Estado = EstadoRSVP.Confirmado;

                await _context.SaveChangesAsync();

                return View("Aceptado");
            }

            return RedirectToAction("Login", "Account", new { returnToken = t, email = correo });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Reject(string t)
        {
            if (string.IsNullOrEmpty(t))
                return BadRequest();

            try
            {
                var protectedBytes = Convert.FromBase64String(System.Net.WebUtility.UrlDecode(t));
                var bytes = _protector.Unprotect(protectedBytes);
                var json = Encoding.UTF8.GetString(bytes);

                var doc = System.Text.Json.JsonDocument.Parse(json);
                int invitacionId = doc.RootElement.GetProperty("InvitacionId").GetInt32();

                var invitacion = await _context.Invitaciones.FindAsync(invitacionId);

                if (invitacion == null)
                    return NotFound();

                if (invitacion.Estado == EstadoRSVP.Confirmado)
                    return Content("Ya fue aceptada");

                if (invitacion.Estado == EstadoRSVP.Rechazado)
                    return Content("Ya fue rechazada");

                invitacion.Estado = EstadoRSVP.Rechazado;

                await _context.SaveChangesAsync();

                return View("Rechazado");
            }
            catch
            {
                return BadRequest();
            }
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

        // GET: Invitacions/MyEvents
        public async Task<IActionResult> MyEvents()
        {
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            // Obtener eventos a los que el usuario fue invitado
            var eventos = await _context.Invitaciones
                .Where(i => i.UsuarioId == userId)
                .Include(i => i.Evento)
                .Select(i => i.Evento!)
                .Where(e => e != null)
                .Distinct()
                .ToListAsync();

            return View("MyEvents", eventos);
        }

        // GET: Invitacions/Invitados?eventoId=1&q=correo
        [HttpGet]
        public async Task<IActionResult> Invitados(int eventoId, string? q)
        {
            // verificar que el evento existe
            var evento = await _context.Eventos.FindAsync(eventoId);
            if (evento == null) return NotFound();

            // solo el organizador del evento o admin puede consultar la lista completa
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            int.TryParse(userIdClaim, out var currentUserId);
            var isOrganizer = (User.IsInRole("Organizador") && evento.OrganizadorId == currentUserId) || User.IsInRole("Admin");

            if (!isOrganizer)
                return Forbid();

            var query = _context.Invitaciones
                .Where(i => i.EventoId == eventoId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(q))
            {
                var qLower = q.Trim().ToLower();
                query = query.Where(i => i.CorreoInvitado != null && i.CorreoInvitado.ToLower().Contains(qLower));
            }

            var invitados = await query
                .Select(i => new {
                    i.InvitacionId,
                    i.CorreoInvitado,
                    Estado = i.Estado.ToString()
                })
                .ToListAsync();

            return Json(new { Evento = new { evento.EventoId, evento.Nombre }, Invitados = invitados });
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
            Console.WriteLine("UserId claim: " + userId);
            Console.WriteLine("Eventos encontrados: " + eventos.Count);
            foreach (var ev in eventos)
            {
                Console.WriteLine($"Evento: id={ev.EventoId}, nombre={ev.Nombre}, organizadorId={ev.OrganizadorId}");
            }

            // Lista de usuarios para seleccionar el correo del invitado (valor = Email)
            var usuarios = _context.Usuarios
                .Select(u => new { u.Email, Display = (u.NombreCompleto ?? u.Email) })
                .ToList();

            ViewBag.Usuarios = new SelectList(usuarios, "Email", "Display");
            ViewBag.EventoId = new SelectList(eventos.Select(e => new { e.EventoId, Nombre = e.Nombre ?? "(sin nombre)" }), "EventoId", "Nombre");
            // Provide detailed event list for the view (name, date, time, place)
            ViewBag.EventosDetailed = eventos.Select(e => new {
                e.EventoId,
                Nombre = e.Nombre ?? "(sin nombre)",
                Fecha = e.Fecha,
                Hora = e.Hora,
                Lugar = e.Lugar
            }).ToList();

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

                var usuarios = _context.Usuarios
                    .Select(u => new { u.Email, Display = (u.NombreCompleto ?? u.Email) })
                    .ToList();

                ViewBag.Usuarios = new SelectList(usuarios, "Email", "Display");
                var eventosErr = _context.Eventos.Where(e => e.OrganizadorId == userId)
                    .Select(e => new { e.EventoId, Nombre = e.Nombre ?? "(sin nombre)", Fecha = e.Fecha, Hora = e.Hora, Lugar = e.Lugar })
                    .ToList();
                ViewBag.EventoId = new SelectList(eventosErr.Select(e => new { e.EventoId, e.Nombre }), "EventoId", "Nombre", invitacion.EventoId);
                ViewBag.EventosDetailed = eventosErr;

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

            // Si el usuario no existe, crear un usuario temporal (invitado) para poder vincular la invitación
            if (usuario == null)
            {
                var hasher = new PasswordHasher<Usuario>();
                var nuevo = new Usuario
                {
                    Email = correo,
                    NombreCompleto = correo,
                    Telefono = null
                };
                // Generar password aleatorio hasheado para cumplir la restricción de PasswordHash no nulo
                nuevo.PasswordHash = hasher.HashPassword(nuevo, System.Guid.NewGuid().ToString());

                _context.Usuarios.Add(nuevo);
                await _context.SaveChangesAsync();

                usuario = nuevo;
            }

            // ✅ ASIGNAR USUARIO (existente o recién creado)
            invitacion.UsuarioId = usuario.UsuarioId;

            invitacion.Estado = EstadoRSVP.Pendiente;

            // ✅ GUARDAR
            _context.Invitaciones.Add(invitacion);
            await _context.SaveChangesAsync();

            // 🔐 Generar token protegido para enlace de aceptación
            var payload = System.Text.Json.JsonSerializer.Serialize(new { invitacion.InvitacionId, Correo = invitacion.CorreoInvitado, EventoId = invitacion.EventoId, ts = DateTime.UtcNow.Ticks });
            var protectedBytes = _protector.Protect(Encoding.UTF8.GetBytes(payload));
            var protectedPayload = Convert.ToBase64String(protectedBytes);
            var token = System.Net.WebUtility.UrlEncode(protectedPayload);
            var acceptUrl = Url.Action("Accept", "Invitacions", new { t = token }, Request.Scheme);
            var rejectUrl = Url.Action("Reject", "Invitacions", new { t = token }, Request.Scheme);
            // Enviar correo (si está configurado) o loguear
            try
            {
                var smtpSection = _config.GetSection("Smtp");
                var host = smtpSection["Host"];
                if (!string.IsNullOrEmpty(host))
                {
                    var port = int.TryParse(smtpSection["Port"], out var p) ? p : 25;
                    var user = smtpSection["User"];
                    var pass = smtpSection["Pass"];
                    var from = smtpSection["From"] ?? "no-reply@example.com";

                    var organizador = await _context.Usuarios.FindAsync(evento.OrganizadorId);
                    // Construir mensaje con MimeKit
                    var mime = new MimeMessage();
                    var smtpFrom = from;
                    if (string.IsNullOrEmpty(smtpFrom) && !string.IsNullOrEmpty(user)) smtpFrom = user;

                    // From display name: organizador nombre or smtpFrom
                    var fromName = organizador?.NombreCompleto ?? smtpFrom;
                    mime.From.Add(new MailboxAddress(fromName, smtpFrom));
                    // If sender should be the smtp user, set Sender
                    if (!string.IsNullOrEmpty(user) && !string.Equals(smtpFrom, user, System.StringComparison.OrdinalIgnoreCase))
                    {
                        mime.Sender = MailboxAddress.Parse(user);
                    }

                    // Reply-To to organizer
                    if (!string.IsNullOrEmpty(organizador?.Email))
                    {
                        mime.ReplyTo.Add(MailboxAddress.Parse(organizador.Email));
                    }

                    mime.To.Add(MailboxAddress.Parse(invitacion.CorreoInvitado));
                    mime.Subject = $"Invitación al evento: {evento.Nombre}";

                    var bodyBuilder = new BodyBuilder();

                    if (!string.IsNullOrEmpty(evento.Images))
                    {
                        var firstImage = evento.Images.Split(';')[0];
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", firstImage);

                        if (System.IO.File.Exists(filePath))
                        {
                            var image = bodyBuilder.LinkedResources.Add(filePath);
                            image.ContentId = MimeKit.Utils.MimeUtils.GenerateMessageId();

                            bodyBuilder.HtmlBody =
                                $"<h2>📅 Invitación a Evento</h2>" +

                                $"<img src='cid:{image.ContentId}' style='width:100%;max-width:600px;border-radius:10px;margin-bottom:15px;' />" +

                                $"<p>Has sido invitado al evento '<strong>{evento.Nombre}</strong>' por {organizador?.NombreCompleto ?? "el organizador"}.</p>" +

                                $"<hr/>" +

                                $"<p><strong>📌 Nombre:</strong> {evento.Nombre}</p>" +
                                $"<p><strong>📅 Fecha:</strong> {evento.Fecha:d}</p>" +
                                $"<p><strong>⏰ Hora:</strong> {evento.Hora}</p>" +
                                $"<p><strong>📍 Lugar:</strong> {evento.Lugar}</p>" +
                                $"<p><strong>📝 Descripción:</strong> {evento.Descripcion}</p>" +
                                $"<p><strong>📂 Categoría:</strong> {evento.Categoria}</p>" +
                                $"<p><strong>👥 Máx Invitados:</strong> {evento.MaximoInvitados}</p>" +

                                $"<br/><br/>" +
                        // 🔥 BOTONES
                            $"<a href='{acceptUrl}' style='padding:12px 20px; background:#22c55e; color:white; text-decoration:none; border-radius:6px; margin-right:10px; display:inline-block;'>✅ Aceptar</a>" +
                            $"<a href='{rejectUrl}' style='padding:12px 20px; background:#ef4444; color:white; text-decoration:none; border-radius:6px; display:inline-block;'>❌ Rechazar</a>";
                        }
                    }

                    mime.Body = bodyBuilder.ToMessageBody();

                    // Registrar destinatario para depuración
                    Console.WriteLine($"Enviando invitación via MailKit: from={smtpFrom}, sender={mime.Sender?.Address}, to={invitacion.CorreoInvitado}");

                    // Enviar con MailKit
                    using var smtpClient = new MailKit.Net.Smtp.SmtpClient();
                    var enableSsl = bool.TryParse(smtpSection["EnableSsl"], out var s) ? s : true;
                    var secure = enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                    smtpClient.Timeout = 10000;
                    await smtpClient.ConnectAsync(host, port, secure);
                    if (!string.IsNullOrEmpty(user))
                    {
                        await smtpClient.AuthenticateAsync(user, pass);
                    }
                    await smtpClient.SendAsync(mime);
                    await smtpClient.DisconnectAsync(true);
                }
                else
                {
                    Console.WriteLine("Enviar enlace de invitación: " + acceptUrl);
                }
            }
            catch (System.Exception ex)
            {
                // Log full exception to help troubleshooting (authentication, network, provider errors)
                Console.WriteLine("Error enviando correo: " + ex.ToString());
                Console.WriteLine("Enlace de invitación: " + acceptUrl);
            }

            Console.WriteLine("INVITACION GUARDADA");
            return RedirectToAction(nameof(Index));
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