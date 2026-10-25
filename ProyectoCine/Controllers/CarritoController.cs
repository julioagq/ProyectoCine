using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCine.Data;
using ProyectoCine.Models;
using ProyectoCine.Services;

namespace ProyectoCine.Controllers
{
    [Authorize(Roles = "Comprador,Administrador")]
    public class CarritoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CarritoController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            await LiberarReservasExpiradasAsync();

            var userId = _userManager.GetUserId(User);
            var items = await _context.CarritoItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var viewModel = new CarritoViewModel { Items = items };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(int funcionId, string butacaCodigo)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            await LiberarReservasExpiradasAsync();

            var funcion = await _context.Funciones
                .Include(f => f.Pelicula)
                .Include(f => f.Sala)
                .FirstOrDefaultAsync(f => f.Id == funcionId);

            if (funcion == null || funcion.Pelicula == null || funcion.Sala == null)
            {
                return NotFound();
            }

            if (!ButacaHelper.TryNormalizarButaca(butacaCodigo, funcion.Sala.Capacidad, out var butacaNormalizada))
            {
                TempData["ErrorCarrito"] = "La butaca no es válida para la sala seleccionada.";
                return RedirectToAction("Details", "Funciones", new { id = funcionId });
            }

            var itemExistente = await _context.CarritoItems
                .FirstOrDefaultAsync(c => c.UserId == userId
                                          && c.FuncionId == funcionId
                                          && c.ButacaCodigo == butacaNormalizada);

            if (itemExistente != null)
            {
                TempData["ErrorCarrito"] = "Esa butaca ya está en tu carrito.";
                return RedirectToAction(nameof(Index));
            }

            var ahora = DateTime.UtcNow;
            var reserva = await _context.ReservasButacas
                .FirstOrDefaultAsync(r => r.FuncionId == funcionId && r.ButacaCodigo == butacaNormalizada);

            if (reserva != null)
            {
                var bloqueadaPorOtro = reserva.Estado == "Pagada" ||
                                       (reserva.Estado == "Pendiente" && reserva.ExpiraEn > ahora && reserva.UserId != userId);

                if (bloqueadaPorOtro)
                {
                    TempData["ErrorCarrito"] = "La butaca seleccionada ya está reservada o vendida.";
                    return RedirectToAction("Details", "Funciones", new { id = funcionId });
                }

                reserva.UserId = userId;
                reserva.Estado = "Pendiente";
                reserva.CreadaEn = ahora;
                reserva.ExpiraEn = ahora.AddMinutes(10);
                _context.ReservasButacas.Update(reserva);
            }
            else
            {
                reserva = new ReservaButaca
                {
                    FuncionId = funcionId,
                    ButacaCodigo = butacaNormalizada,
                    UserId = userId,
                    Estado = "Pendiente",
                    CreadaEn = ahora,
                    ExpiraEn = ahora.AddMinutes(10)
                };

                _context.ReservasButacas.Add(reserva);
            }

            var nuevoItem = new CarritoItem
            {
                UserId = userId,
                FuncionId = funcionId,
                PeliculaId = funcion.PeliculaId,
                TituloPelicula = funcion.Pelicula.Titulo,
                ButacaCodigo = butacaNormalizada,
                Precio = funcion.Precio,
                Cantidad = 1
            };

            _context.CarritoItems.Add(nuevoItem);

            await _context.SaveChangesAsync();
            TempData["MensajeExito"] = $"Butaca {butacaNormalizada} agregada al carrito por 10 minutos.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarMultiple(int funcionId, string butacasSeleccionadas, int cantidadDeseada)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            await LiberarReservasExpiradasAsync();

            var funcion = await _context.Funciones
                .Include(f => f.Pelicula)
                .Include(f => f.Sala)
                .FirstOrDefaultAsync(f => f.Id == funcionId);

            if (funcion == null || funcion.Pelicula == null || funcion.Sala == null)
            {
                return NotFound();
            }

            var butacasCrudas = (butacasSeleccionadas ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (!butacasCrudas.Any())
            {
                TempData["ErrorCarrito"] = "Debes seleccionar al menos una butaca.";
                return RedirectToAction("Details", "Funciones", new { id = funcionId });
            }

            var butacasNormalizadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var butaca in butacasCrudas)
            {
                if (!ButacaHelper.TryNormalizarButaca(butaca, funcion.Sala.Capacidad, out var butacaNormalizada))
                {
                    TempData["ErrorCarrito"] = "Una o más butacas no son válidas para la sala seleccionada.";
                    return RedirectToAction("Details", "Funciones", new { id = funcionId });
                }

                butacasNormalizadas.Add(butacaNormalizada);
            }

            if (cantidadDeseada > 0 && butacasNormalizadas.Count > cantidadDeseada)
            {
                TempData["ErrorCarrito"] = "No puedes seleccionar más butacas de las solicitadas.";
                return RedirectToAction("Details", "Funciones", new { id = funcionId });
            }

            var butacas = butacasNormalizadas.ToList();
            var ahora = DateTime.UtcNow;

            var yaEnCarrito = await _context.CarritoItems
                .Where(c => c.UserId == userId && c.FuncionId == funcionId && butacas.Contains(c.ButacaCodigo!))
                .Select(c => c.ButacaCodigo!)
                .ToListAsync();

            if (yaEnCarrito.Any())
            {
                TempData["ErrorCarrito"] = $"Estas butacas ya están en tu carrito: {string.Join(", ", yaEnCarrito.OrderBy(x => x))}.";
                return RedirectToAction("Details", "Funciones", new { id = funcionId });
            }

            var reservasExistentes = await _context.ReservasButacas
                .Where(r => r.FuncionId == funcionId && butacas.Contains(r.ButacaCodigo))
                .ToListAsync();

            foreach (var butaca in butacas)
            {
                var reserva = reservasExistentes.FirstOrDefault(r => r.ButacaCodigo == butaca);
                if (reserva == null)
                {
                    continue;
                }

                var bloqueadaPorOtro = reserva.Estado == "Pagada" ||
                                       (reserva.Estado == "Pendiente" && reserva.ExpiraEn > ahora && reserva.UserId != userId);

                if (bloqueadaPorOtro)
                {
                    TempData["ErrorCarrito"] = $"La butaca {butaca} ya está reservada o vendida.";
                    return RedirectToAction("Details", "Funciones", new { id = funcionId });
                }
            }

            foreach (var butaca in butacas)
            {
                var reserva = reservasExistentes.FirstOrDefault(r => r.ButacaCodigo == butaca);
                if (reserva != null)
                {
                    reserva.UserId = userId;
                    reserva.Estado = "Pendiente";
                    reserva.CreadaEn = ahora;
                    reserva.ExpiraEn = ahora.AddMinutes(10);
                    _context.ReservasButacas.Update(reserva);
                }
                else
                {
                    _context.ReservasButacas.Add(new ReservaButaca
                    {
                        FuncionId = funcionId,
                        ButacaCodigo = butaca,
                        UserId = userId,
                        Estado = "Pendiente",
                        CreadaEn = ahora,
                        ExpiraEn = ahora.AddMinutes(10)
                    });
                }

                _context.CarritoItems.Add(new CarritoItem
                {
                    UserId = userId,
                    FuncionId = funcionId,
                    PeliculaId = funcion.PeliculaId,
                    TituloPelicula = funcion.Pelicula.Titulo,
                    ButacaCodigo = butaca,
                    Precio = funcion.Precio,
                    Cantidad = 1
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorCarrito"] = "No fue posible reservar todas las butacas. Intenta nuevamente.";
                return RedirectToAction("Details", "Funciones", new { id = funcionId });
            }

            TempData["MensajeExito"] = $"{butacas.Count} butaca(s) agregadas al carrito por 10 minutos.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarPelicula(int peliculaId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var pelicula = await _context.Peliculas.FirstOrDefaultAsync(p => p.Id == peliculaId);
            if (pelicula == null)
            {
                return NotFound();
            }

            var itemExistente = await _context.CarritoItems.FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.PeliculaId == peliculaId &&
                c.FuncionId == null &&
                c.ButacaCodigo == null);

            if (itemExistente != null)
            {
                itemExistente.Cantidad += 1;
                _context.CarritoItems.Update(itemExistente);
            }
            else
            {
                var nuevoItem = new CarritoItem
                {
                    UserId = userId,
                    PeliculaId = pelicula.Id,
                    TituloPelicula = pelicula.Titulo,
                    Precio = pelicula.Precio,
                    Cantidad = 1
                };

                _context.CarritoItems.Add(nuevoItem);
            }

            await _context.SaveChangesAsync();
            TempData["MensajeExito"] = $"{pelicula.Titulo} agregada al carrito.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarCantidad(int id, int cantidad)
        {
            if (cantidad <= 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var item = await _context.CarritoItems.FindAsync(id);
            if (item == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            if (item.UserId != userId)
            {
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(item.ButacaCodigo))
            {
                item.Cantidad = 1;
            }
            else
            {
                item.Cantidad = cantidad;
            }

            _context.Update(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var item = await _context.CarritoItems.FindAsync(id);
            if (item == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            if (item.UserId != userId)
            {
                return RedirectToAction(nameof(Index));
            }

            if (item.FuncionId.HasValue && !string.IsNullOrWhiteSpace(item.ButacaCodigo))
            {
                var reserva = await _context.ReservasButacas
                    .FirstOrDefaultAsync(r => r.FuncionId == item.FuncionId.Value
                                              && r.ButacaCodigo == item.ButacaCodigo
                                              && r.UserId == userId
                                              && r.Estado == "Pendiente");

                if (reserva != null)
                {
                    reserva.Estado = "Liberada";
                    reserva.ExpiraEn = DateTime.UtcNow;
                    _context.ReservasButacas.Update(reserva);
                }
            }

            try
            {
                _context.CarritoItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // El ítem ya fue eliminado/modificado por otra solicitud concurrente.
                // Se considera estado final consistente para esta operación.
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LiberarReservasExpiradasAsync()
        {
            var ahora = DateTime.UtcNow;
            var expiradas = await _context.ReservasButacas
                .Where(r => r.Estado == "Pendiente" && r.ExpiraEn <= ahora)
                .ToListAsync();

            if (!expiradas.Any())
            {
                return;
            }

            foreach (var reserva in expiradas)
            {
                reserva.Estado = "Liberada";
            }

            var expiradasLookup = expiradas
                .Select(r => new { r.FuncionId, r.ButacaCodigo, r.UserId })
                .ToList();

            foreach (var key in expiradasLookup)
            {
                var item = await _context.CarritoItems.FirstOrDefaultAsync(c =>
                    c.FuncionId == key.FuncionId &&
                    c.ButacaCodigo == key.ButacaCodigo &&
                    c.UserId == key.UserId);

                if (item != null)
                {
                    _context.CarritoItems.Remove(item);
                }
            }

            await _context.SaveChangesAsync();
        }


    }
}
