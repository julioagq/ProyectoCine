using Microsoft.AspNetCore.Mvc; // Controladores y vistas
using QRCoder; // Librería para generar códigos QR

namespace ProyectoCine.Controllers
{
    public class TicketsController : Controller
    {
        // Acción que genera un código QR en formato PNG
        [HttpGet]
        public IActionResult GenerarQR(string codigo)
        {
            // Si no se recibe código, se usa uno de prueba
            if (string.IsNullOrEmpty(codigo))
            {
                codigo = "TICKET-DEMO-2026";
            }

            // Construye la URL que apunta a la validación del ticket
            string urlValidacion = $"{Request.Scheme}://{Request.Host}/Tickets/Validar?codigo={codigo}";

            // Genera el QR usando la librería QRCoder
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(urlValidacion, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                // Convierte el QR en bytes de imagen PNG
                byte[] qrCodeBytes = qrCode.GetGraphic(20);

                // Devuelve la imagen como archivo PNG
                return File(qrCodeBytes, "image/png");
            }
        }

        // Acción que valida el ticket cuando se escanea el QR
        [HttpGet]
        public IActionResult Validar(string codigo)
        {
            // Se pasa el código a la vista
            ViewBag.Codigo = codigo;

            // Simulación de validación: válido si el código no está vacío
            ViewBag.EsValido = !string.IsNullOrEmpty(codigo);

            // Devuelve la vista de validación
            return View();
        }
    }
}
