using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCine.Data;
using ProyectoCine.Models;

namespace ProyectoCine.Controllers
{
    [Authorize(Roles = "Comprador,Administrador")]
    public class PerfilController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PerfilController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return Challenge();
            }

            var esAdmin = User.IsInRole("Administrador") || User.IsInRole("Admin");

            var ordenesQuery = _context.Ordenes
                .Include(o => o.Detalles)
                .AsQueryable();

            if (!esAdmin)
            {
                ordenesQuery = ordenesQuery.Where(o => o.UserId == usuario.Id);
            }

            var ordenes = await ordenesQuery
                .OrderByDescending(o => o.FechaOrden)
                .ToListAsync();

            var reservasQuery = _context.ReservasButacas.AsQueryable();
            if (!esAdmin)
            {
                reservasQuery = reservasQuery.Where(r => r.UserId == usuario.Id);
            }

            var reservasPendientes = await reservasQuery
                .Where(r => r.Estado == "Pendiente")
                .OrderByDescending(r => r.CreadaEn)
                .ToListAsync();

            var reservasPagadas = await reservasQuery
                .Where(r => r.Estado == "Pagada")
                .OrderByDescending(r => r.CreadaEn)
                .ToListAsync();

            var userIds = reservasPendientes
                .Select(r => r.UserId)
                .Concat(reservasPagadas.Select(r => r.UserId))
                .Concat(ordenes.Select(o => o.UserId))
                .Distinct()
                .ToList();

            var usuariosPorId = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, Nombre = u.Email ?? u.UserName ?? "Sin correo" })
                .ToDictionaryAsync(x => x.Id, x => x.Nombre);

            var vm = new PerfilViewModel
            {
                Usuario = usuario,
                EsAdmin = esAdmin,
                Ordenes = ordenes,
                ReservasPendientes = reservasPendientes,
                ReservasPagadas = reservasPagadas,
                UsuariosPorId = usuariosPorId
            };

            return View(vm);
        }

        public async Task<IActionResult> DetalleCompra(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var esAdmin = User.IsInRole("Administrador") || User.IsInRole("Admin");

            var orden = await _context.Ordenes
                .Include(o => o.Detalles)
                .FirstOrDefaultAsync(o => o.Id == id && (esAdmin || o.UserId == userId));

            if (orden == null)
            {
                return NotFound();
            }

            return View(orden);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarReserva(int id)
        {
            var esAdmin = User.IsInRole("Administrador") || User.IsInRole("Admin");
            if (!esAdmin)
            {
                return Forbid();
            }

            var reserva = await _context.ReservasButacas.FirstOrDefaultAsync(r => r.Id == id);
            if (reserva == null)
            {
                TempData["ErrorPerfil"] = "La reserva no existe o ya fue eliminada.";
                return RedirectToAction(nameof(Index));
            }

            _context.ReservasButacas.Remove(reserva);
            await _context.SaveChangesAsync();

            TempData["MensajePerfil"] = $"Reserva {reserva.ButacaCodigo} de la función #{reserva.FuncionId} eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmarEliminarCompra(int id)
        {
            var esAdmin = User.IsInRole("Administrador") || User.IsInRole("Admin");
            if (!esAdmin)
            {
                return Forbid();
            }

            var orden = await _context.Ordenes
                .Include(o => o.Detalles)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null)
            {
                TempData["ErrorPerfil"] = "La compra no existe o ya fue eliminada.";
                return RedirectToAction(nameof(Index));
            }

            return View(orden);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCompra(int id)
        {
            var esAdmin = User.IsInRole("Administrador") || User.IsInRole("Admin");
            if (!esAdmin)
            {
                return Forbid();
            }

            var orden = await _context.Ordenes
                .Include(o => o.Detalles)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null)
            {
                TempData["ErrorPerfil"] = "La compra no existe o ya fue eliminada.";
                return RedirectToAction(nameof(Index));
            }

            _context.DetalleOrdenes.RemoveRange(orden.Detalles);
            _context.Ordenes.Remove(orden);
            await _context.SaveChangesAsync();

            TempData["MensajePerfil"] = $"Compra #{id} eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
