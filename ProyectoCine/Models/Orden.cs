using System;
using System.Collections.Generic;

namespace ProyectoCine.Models
{
    public class Orden
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime FechaOrden { get; set; } = DateTime.Now;
        public decimal TotalGeneral { get; set; }
        public string TransaccionId { get; set; } = string.Empty;

        public List<DetalleOrden> Detalles { get; set; } = new List<DetalleOrden>();
    }

    public class DetalleOrden
    {
        public int Id { get; set; }
        public int OrdenId { get; set; }
        public Orden Orden { get; set; } = null!;

        public int PeliculaId { get; set; }
        public string TituloPelicula { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal SubTotal { get; set; }
    }
}