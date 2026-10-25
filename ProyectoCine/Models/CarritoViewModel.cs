using System.Collections.Generic;
using System.Linq;

namespace ProyectoCine.Models
{
    public class CarritoViewModel
    {
        // Cambiar ItemCarrito por CarritoItem aquí:
        public List<CarritoItem> Items { get; set; } = new List<CarritoItem>();

        // El Total General lo calcula el modelo sumando los subtotales
        public decimal TotalGeneral
        {
            get { return Items.Sum(item => item.SubTotal); }
        }
    }
}