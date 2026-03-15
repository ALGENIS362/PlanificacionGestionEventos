using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlanificacionGestionEventos.Data;
using PlanificacionGestionEventos.Models;
using Microsoft.EntityFrameworkCore;

namespace PlanificacionGestionEventos.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                return View(model);
            }

            // Verificar contraseña usando PasswordHasher
            var hasher = new PasswordHasher<Usuario>();
            var verifyResult = hasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, model.Password);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
                return View(model);
            }

            // Cargar roles del usuario y agregarlos como claims
            var roleNames = await _context.UsuariosRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UsuarioId == user.UsuarioId)
                .Select(ur => ur.Role.Nombre)
                .ToListAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, user.NombreCompleto ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var rn in roleNames)
            {
                claims.Add(new Claim(ClaimTypes.Role, rn));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // check if email already exists
            if (await _context.Usuarios.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "El correo ya está registrado.");
                return View(model);
            }

            var usuario = new Usuario
            {
                NombreCompleto = model.Name,
                Email = model.Email,
                Telefono = model.Telefono,
                // Guardar password hasheada de forma segura
                PasswordHash = new PasswordHasher<Usuario>().HashPassword(null, model.Password)
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Asignar rol seleccionado
            var roleName = model.Role ?? "Invitado";
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == roleName);
            if (role == null)
            {
                role = new Role { Nombre = roleName };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            var usuarioRole = new UsuarioRole { UsuarioId = usuario.UsuarioId, RoleId = role.RoleId };
            _context.UsuariosRoles.Add(usuarioRole);
            await _context.SaveChangesAsync();

            // Autenticar al usuario recién registrado, incluyendo su rol
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto ?? usuario.Email),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, roleName)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
