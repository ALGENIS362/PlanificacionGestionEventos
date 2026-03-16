using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PlanificacionGestionEventos.Data;
using PlanificacionGestionEventos.Models;

namespace PlanificacionGestionEventos.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
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
            var usuarios = await (
     from u in _context.Usuarios
     join ur in _context.UsuariosRoles on u.UsuarioId equals ur.UsuarioId into userRoles
     from ur in userRoles.DefaultIfEmpty()
     join r in _context.Roles on ur.RoleId equals r.RoleId into roles
     from r in roles.DefaultIfEmpty()
     select new Models.UsuarioListViewModel
     {
         Usuario = u,
         RoleName = r != null && r.Nombre != null ? r.Nombre : ""
     }
         ).ToListAsync();
           return View(usuarios);
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(m => m.UsuarioId == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // GET: Usuarios/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Roles"] = await _context.Roles.Select(r => r.Nombre).ToListAsync();
            return View(new Models.UsuarioCreateViewModel());
        }

        // POST: Usuarios/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Models.UsuarioCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Roles"] = await _context.Roles.Select(r => r.Nombre).ToListAsync();
                return View(model);
            }

            // Crear entidad Usuario
            var usuario = new Usuario
            {
                NombreCompleto = model.NombreCompleto,
                Email = model.Email,
                Telefono = model.Telefono
            };

            // Hashear contraseña
            var hasher = new PasswordHasher<Usuario>();

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                usuario.PasswordHash = hasher.HashPassword(usuario, model.Password);
            }

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Asignar rol seleccionado o por defecto Invitado
            var roleName = string.IsNullOrWhiteSpace(model.SelectedRole) ? "Invitado" : model.SelectedRole;
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == roleName);
            if (role == null)
            {
                role = new Role { Nombre = roleName };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            _context.UsuariosRoles.Add(new UsuarioRole { UsuarioId = usuario.UsuarioId, RoleId = role.RoleId });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            var vm = new Models.UsuarioEditViewModel
            {
                UsuarioId = usuario.UsuarioId,
                NombreCompleto = usuario.NombreCompleto,
                Email = usuario.Email,
                Telefono = usuario.Telefono
            };

            // obtener rol actual si existe
            var roleName = await _context.UsuariosRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UsuarioId == usuario.UsuarioId)
                .Select(ur => ur.Role!.Nombre ?? "")
                .FirstOrDefaultAsync();

            vm.SelectedRole = roleName;

            // lista de roles para dropdown: asegurar que Invitado y Organizador (y Admin) estén disponibles
            var dbRoles = await _context.Roles.Select(r => r.Nombre).ToListAsync();
            var roles = new List<string> { "Invitado", "Organizador", "Admin" };
            foreach (var r in dbRoles)
            {
                if (r != null && !roles.Contains(r))
                    roles.Add(r);
            }
            ViewData["Roles"] = roles;

            return View(vm);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Models.UsuarioEditViewModel model)
        {
            if (id != model.UsuarioId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
                return View(model);

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            usuario.NombreCompleto = model.NombreCompleto;
            usuario.Email = model.Email;
            usuario.Telefono = model.Telefono;

            // Si se proporciona una nueva contraseña, hashearla y actualizar
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Usuario>();
                usuario.PasswordHash = hasher.HashPassword(usuario, model.Password);
            }

            try
            {
                _context.Update(usuario);
                await _context.SaveChangesAsync();

                // actualizar rol si se indicó
                if (!string.IsNullOrWhiteSpace(model.SelectedRole))
                {
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == model.SelectedRole);
                    if (role == null)
                    {
                        role = new Role { Nombre = model.SelectedRole };
                        _context.Roles.Add(role);
                        await _context.SaveChangesAsync();
                    }

                    // eliminar roles previos
                    var existing = _context.UsuariosRoles.Where(ur => ur.UsuarioId == usuario.UsuarioId);
                    _context.UsuariosRoles.RemoveRange(existing);
                    await _context.SaveChangesAsync();

                    // asignar nuevo rol
                    _context.UsuariosRoles.Add(new UsuarioRole { UsuarioId = usuario.UsuarioId, RoleId = role.RoleId });
                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(usuario.UsuarioId))
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

        // GET: Usuarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(m => m.UsuarioId == id);
            if (usuario == null)
            {
                return NotFound();
            }

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
                _context.Usuarios.Remove(usuario);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.UsuarioId == id);
        }
    }
}