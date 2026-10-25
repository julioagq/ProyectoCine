using System;
using System.Threading.Tasks;

namespace ProyectoCine.Services
{
    public class PasarelaPagoSimulada : IPasarelaPago
    {
        public async Task<ResultadoPago> ProcesarPagoAsync(string numeroTarjeta, decimal monto, string tarjetaHabiente)
        {
            await Task.Delay(500); // Simula el tiempo de respuesta del banco

            // Regla simple: Se aprueba si la tarjeta tiene al menos 12 dígitos y el monto es mayor a 0
            if (!string.IsNullOrWhiteSpace(numeroTarjeta) && numeroTarjeta.Replace(" ", "").Length >= 12 && monto > 0)
            {
                return new ResultadoPago
                {
                    Aprobado = true,
                    Descripcion = "Pago aprobado exitosamente por la pasarela simulada.",
                    IdTransaccion = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
                };
            }

            return new ResultadoPago
            {
                Aprobado = false,
                Descripcion = "Pago rechazado. Por favor verifique los datos de la tarjeta.",
                IdTransaccion = string.Empty
            };
        }
    }
}