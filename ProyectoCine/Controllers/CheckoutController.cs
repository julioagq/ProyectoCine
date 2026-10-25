using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCine.Data;
using ProyectoCine.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace ProyectoCine.Controllers
{
    [Authorize(Roles = "Comprador,Administrador")]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender? _emailSender;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IHttpClientFactory httpClientFactory,
            ILogger<CheckoutController> logger,
            IEmailSender? emailSender = null)
        {
            _context = context;
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            await LiberarReservasExpiradasAsync();

            var userId = _userManager.GetUserId(User);
            var items = await _context.CarritoItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!items.Any())
            {
                return RedirectToAction("Index", "Carrito");
            }

            var viewModel = new CarritoViewModel { Items = items };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarPago(string numeroTarjeta, string nombreTarjeta)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var userId = user.Id;
            await LiberarReservasExpiradasAsync();

            var items = await _context.CarritoItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!items.Any())
            {
                return RedirectToAction("Index", "Carrito");
            }

            var reservasValidas = await ValidarReservasCarritoAsync(userId, items);
            if (!reservasValidas)
            {
                TempData["ErrorPago"] = "Una o más butacas ya no están disponibles. Revise su carrito.";
                return RedirectToAction("Index", "Carrito");
            }

            var total = items.Sum(i => i.SubTotal);

            var client = _httpClientFactory.CreateClient();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var endpoint = $"{baseUrl}/api/pagos/procesar";

            try
            {
                var respuesta = await client.PostAsJsonAsync(endpoint, new
                {
                    NumeroTarjeta = numeroTarjeta,
                    Monto = total,
                    TarjetaHabiente = nombreTarjeta
                });

                if (!respuesta.IsSuccessStatusCode)
                {
                    await LiberarReservasPendientesUsuarioAsync(userId);

                    TempData["ErrorPago"] = respuesta.StatusCode == HttpStatusCode.PaymentRequired
                        ? "El pago fue rechazado por la pasarela."
                        : "No fue posible procesar el pago en este momento.";

                    _logger.LogWarning("Pago rechazado o fallido. Usuario: {UserId}. Estado HTTP: {StatusCode}", userId, (int)respuesta.StatusCode);
                    return RedirectToAction(nameof(Index));
                }

                var payload = await respuesta.Content.ReadFromJsonAsync<ResultadoPagoApi>();
                if (payload == null || !payload.Aprobado)
                {
                    await LiberarReservasPendientesUsuarioAsync(userId);
                    TempData["ErrorPago"] = "La pasarela devolvió una respuesta inválida.";
                    _logger.LogWarning("Respuesta inválida de pasarela para usuario {UserId}", userId);
                    return RedirectToAction(nameof(Index));
                }

                var orden = new Orden
                {
                    UserId = userId,
                    TotalGeneral = total,
                    TransaccionId = payload.TransaccionId ?? string.Empty,
                    FechaOrden = DateTime.Now,
                    Detalles = items.Select(i => new DetalleOrden
                    {
                        PeliculaId = i.PeliculaId,
                        TituloPelicula = string.IsNullOrWhiteSpace(i.ButacaCodigo)
                            ? i.TituloPelicula
                            : $"{i.TituloPelicula} (Butaca {i.ButacaCodigo})",
                        PrecioUnitario = i.Precio,
                        Cantidad = i.Cantidad,
                        SubTotal = i.SubTotal
                    }).ToList()
                };

                _context.Ordenes.Add(orden);
                _context.CarritoItems.RemoveRange(items);
                await MarcarReservasPagadasAsync(userId);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pago aprobado. Usuario: {UserId}. Orden: {OrdenId}. Tx: {TransaccionId}", userId, orden.Id, orden.TransaccionId);

                if (_emailSender != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    var bodyBuilder = new StringBuilder();
                    bodyBuilder.Append($"<h2>¡Gracias por tu compra, {user.UserName}!</h2>");
                    bodyBuilder.Append($"<p><strong>N° de Orden:</strong> #{orden.Id}</p>");
                    bodyBuilder.Append($"<p><strong>Fecha de compra:</strong> {orden.FechaOrden:dd/MM/yyyy HH:mm}</p>");
                    bodyBuilder.Append("<h3>Productos adquiridos:</h3><ul>");

                    foreach (var detalle in orden.Detalles)
                    {
                        bodyBuilder.Append($"<li>{detalle.TituloPelicula} - Cantidad: {detalle.Cantidad} - Subtotal: ${detalle.SubTotal:0.00}</li>");
                    }

                    bodyBuilder.Append($"</ul><p><strong>Total pagado:</strong> ${orden.TotalGeneral:0.00}</p>");

                    try
                    {
                        await _emailSender.SendEmailAsync(user.Email, "Confirmación de Compra - Proyecto Cine", bodyBuilder.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Falló el envío de correo para la orden {OrdenId}", orden.Id);
                    }
                }

                return RedirectToAction("Confirmacion", new { id = orden.Id });
            }
            catch (Exception ex)
            {
                await LiberarReservasPendientesUsuarioAsync(userId);
                _logger.LogError(ex, "Error procesando pago para usuario {UserId}", userId);
                TempData["ErrorPago"] = "Ocurrió un error inesperado al procesar el pago.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Confirmacion(int id)
        {
            var userId = _userManager.GetUserId(User);
            var orden = await _context.Ordenes
                .Include(o => o.Detalles)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (orden == null)
            {
                return NotFound();
            }

            return View(orden);
        }

        private async Task<bool> ValidarReservasCarritoAsync(string userId, List<CarritoItem> items)
        {
            var ahora = DateTime.UtcNow;

            foreach (var item in items.Where(i => i.FuncionId.HasValue && !string.IsNullOrWhiteSpace(i.ButacaCodigo)))
            {
                var reserva = await _context.ReservasButacas
                    .FirstOrDefaultAsync(r => r.FuncionId == item.FuncionId!.Value
                                              && r.ButacaCodigo == item.ButacaCodigo
                                              && r.UserId == userId
                                              && r.Estado == "Pendiente"
                                              && r.ExpiraEn > ahora);

                if (reserva == null)
                {
                    return false;
                }
            }

            return true;
        }

        private async Task MarcarReservasPagadasAsync(string userId)
        {
            var pendientes = await _context.ReservasButacas
                .Where(r => r.UserId == userId && r.Estado == "Pendiente" && r.ExpiraEn > DateTime.UtcNow)
                .ToListAsync();

            foreach (var reserva in pendientes)
            {
                reserva.Estado = "Pagada";
                reserva.ExpiraEn = DateTime.UtcNow.AddYears(10);
            }
        }

        private async Task LiberarReservasPendientesUsuarioAsync(string userId)
        {
            var pendientes = await _context.ReservasButacas
                .Where(r => r.UserId == userId && r.Estado == "Pendiente")
                .ToListAsync();

            foreach (var reserva in pendientes)
            {
                reserva.Estado = "Liberada";
                reserva.ExpiraEn = DateTime.UtcNow;
            }

            var itemsReservados = await _context.CarritoItems
                .Where(c => c.UserId == userId && !string.IsNullOrWhiteSpace(c.ButacaCodigo))
                .ToListAsync();

            if (itemsReservados.Any())
            {
                _context.CarritoItems.RemoveRange(itemsReservados);
            }

            await _context.SaveChangesAsync();
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

            await _context.SaveChangesAsync();
        }

        private sealed class ResultadoPagoApi
        {
            public bool Aprobado { get; set; }
            public string? Descripcion { get; set; }
            public string? TransaccionId { get; set; }
        }
    }
}
