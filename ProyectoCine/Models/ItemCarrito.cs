namespace ProyectoCine.Models
{
    public class CarritoItem
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int PeliculaId { get; set; }
        public int? FuncionId { get; set; }
        public string TituloPelicula { get; set; } = string.Empty;
        public string? ButacaCodigo { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }

        public decimal SubTotal => Precio * Cantidad;
    }
}