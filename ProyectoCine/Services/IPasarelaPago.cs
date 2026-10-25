namespace ProyectoCine.Services
{
    public class ResultadoPago
    {
        public bool Aprobado { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string IdTransaccion { get; set; } = string.Empty;
    }

    public interface IPasarelaPago
    {
        Task<ResultadoPago> ProcesarPagoAsync(string numeroTarjeta, decimal monto, string tarjetaHabiente);
    }
}