using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanificacionGestionEventos.Data;
using PlanificacionGestionEventos.Models;
using System.Security.Claims;

namespace PlanificacionGestionEventos.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var data = await (
                from u in _context.Usuarios
                join ur in _context.UsuariosRoles on u.UsuarioId equals ur.UsuarioId into userRoles
                from ur in userRoles.DefaultIfEmpty()
                join r in _context.Roles on ur.RoleId equals r.RoleId into roles
                from r in roles.DefaultIfEmpty()

                where u.UsuarioId == userId
                select new { u, r }
             ).ToListAsync();

            var usuarios = data.Select(x => new UsuarioListViewModel
            {
                Usuario = x.u,
                RoleName = x.r?.Nombre ?? ""
            }).ToList();

            return View(usuarios);
        }

        // GET: Usuarios/Create
        public IActionResult Create()
        {
            ViewData["Roles"] = new List<string> { "Organizador", "Participante" };
            return View(new Models.UsuarioCreateViewModel());
        }

        // POST: Usuarios/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Models.UsuarioCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Roles"] = new List<string> { "Organizador", "Participante" };
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.Telefono))
            {
                model.Telefono = model.Telefono.Replace("-", "");
            }

            var usuario = new Usuario
            {
                NombreCompleto = model.NombreCompleto,
                Email = model.Email,
                Telefono = model.Telefono
            };

            var hasher = new PasswordHasher<Usuario>();

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                usuario.PasswordHash = hasher.HashPassword(usuario, model.Password);
            }

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // 🔥 ASIGNAR ROL (CORREGIDO)
            var roleName = string.IsNullOrWhiteSpace(model.SelectedRole) ? "Participante" : model.SelectedRole;

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == roleName);

            if (role == null)
            {
                role = new Role { Nombre = roleName };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            _context.UsuariosRoles.Add(new UsuarioRole
            {
                UsuarioId = usuario.UsuarioId,
                RoleId = role.RoleId
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // 🔥 AQUÍ VA
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int currentUserId))
                return Unauthorized();

            if (id != currentUserId)
                return Unauthorized(); // 🔥 NO puede editar otro

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            var vm = new Models.UsuarioEditViewModel
            {
                UsuarioId = usuario.UsuarioId,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Telefono = usuario.Telefono
            };

            return View(vm);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Models.UsuarioEditViewModel model)
        {
            if (id != model.UsuarioId) return NotFound();

            // 🔥 AQUÍ VA
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int currentUserId))
                return Unauthorized();

            if (id != currentUserId)
                return Unauthorized(); // 🔥 NO puede editar otro

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.Telefono))
            {
                model.Telefono = model.Telefono.Replace("-", "");
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.NombreCompleto = model.NombreCompleto;
            usuario.Email = model.Email;
            usuario.Telefono = model.Telefono;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var hasher = new PasswordHasher<Usuario>();
                usuario.PasswordHash = hasher.HashPassword(usuario, model.Password);
            }

            _context.Update(usuario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Usuarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(m => m.UsuarioId == id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario != null)
            {
                var roles = _context.UsuariosRoles.Where(ur => ur.UsuarioId == id);
                _context.UsuariosRoles.RemoveRange(roles);

                _context.Usuarios.Remove(usuario);
            }

            await _context.SaveChangesAsync();
            // Si la petición es AJAX, devolver JSON para que el cliente pueda actualizar la UI sin recargar
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         (Request.Headers.ContainsKey("Accept") && Request.Headers["Accept"].ToString().Contains("application/json"));

            if (isAjax)
            {
                return Json(new { success = true });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}