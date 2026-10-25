using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoCine.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private Task<bool> EsUsuarioAdminAsync()
        {
            var esAdmin = User?.Identity?.IsAuthenticated == true &&
                          (User.IsInRole("Administrador") ||
                           string.Equals(User.Identity?.Name, "admin@cine.com", System.StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(esAdmin);
        }

        // GET: Admin/Index
        public async Task<IActionResult> Index()
        {
            if (!await EsUsuarioAdminAsync())
            {
                TempData["Error"] = "No tienes los permisos necesarios para acceder a esta sección.";
                return RedirectToAction("Index", "Home");
            }

            var users = await _userManager.Users.ToListAsync();
            var userRolesList = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRolesList.Add(new UserRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = roles.ToList()
                });
            }

            return View(userRolesList);
        }

        // POST: Admin/HacerAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HacerAdmin(string userId)
        {
            if (!await EsUsuarioAdminAsync()) return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                if (!await _roleManager.RoleExistsAsync("Administrador"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Administrador"));
                }

                if (!await _userManager.IsInRoleAsync(user, "Administrador"))
                {
                    await _userManager.AddToRoleAsync(user, "Administrador");
                    await _userManager.RemoveFromRoleAsync(user, "Comprador");
                }
            }
            return RedirectToAction("Index");
        }

        // POST: Admin/HacerComprador
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HacerComprador(string userId)
        {
            if (!await EsUsuarioAdminAsync()) return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                if (user.Email == "admin@cine.com")
                {
                    TempData["Error"] = "No se le pueden revocar los permisos al administrador principal.";
                    return RedirectToAction("Index");
                }

                if (!await _roleManager.RoleExistsAsync("Comprador"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Comprador"));
                }

                if (!await _userManager.IsInRoleAsync(user, "Comprador"))
                {
                    await _userManager.AddToRoleAsync(user, "Comprador");
                    await _userManager.RemoveFromRoleAsync(user, "Administrador");
                }
            }
            return RedirectToAction("Index");
        }

        // 👉 NUEVA ACCIÓN: Eliminar Usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarUsuario(string userId)
        {
            if (!await EsUsuarioAdminAsync()) return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                if (user.Email == "admin@cine.com")
                {
                    TempData["Error"] = "No se puede eliminar la cuenta del administrador principal.";
                    return RedirectToAction("Index");
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["Mensaje"] = "El usuario ha sido eliminado exitosamente.";
                }
                else
                {
                    TempData["Error"] = "Hubo un error al intentar eliminar al usuario.";
                }
            }
            return RedirectToAction("Index");
        }
    }

    public class UserRolesViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
    }
}