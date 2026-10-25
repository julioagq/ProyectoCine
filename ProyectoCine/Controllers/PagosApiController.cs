using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoCine.Services;

namespace ProyectoCine.Controllers
{
    [ApiController]
    [Route("api/pagos")]
    [Authorize]
    public class PagosApiController : ControllerBase
    {
        private readonly IPasarelaPago _pasarelaPago;

        public PagosApiController(IPasarelaPago pasarelaPago)
        {
            _pasarelaPago = pasarelaPago;
        }

        [AllowAnonymous]
        [HttpPost("procesar")]
        public async Task<IActionResult> Procesar([FromBody] ProcesarPagoRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.NumeroTarjeta) || request.Monto <= 0)
            {
                return BadRequest(new { mensaje = "Solicitud de pago inválida." });
            }

            var resultado = await _pasarelaPago.ProcesarPagoAsync(request.NumeroTarjeta, request.Monto, request.TarjetaHabiente ?? string.Empty);

            if (!resultado.Aprobado)
            {
                return StatusCode(StatusCodes.Status402PaymentRequired, new
                {
                    aprobado = false,
                    descripcion = resultado.Descripcion
                });
            }

            return Ok(new
            {
                aprobado = true,
                descripcion = resultado.Descripcion,
                transaccionId = resultado.IdTransaccion
            });
        }
    }

    public class ProcesarPagoRequest
    {
        public string NumeroTarjeta { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string? TarjetaHabiente { get; set; }
    }
}
