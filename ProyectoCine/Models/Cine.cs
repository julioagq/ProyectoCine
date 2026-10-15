using System.ComponentModel.DataAnnotations;

namespace ProyectoCine.Models
{
    public class Cine
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La ubicación es obligatoria.")]
        [StringLength(200)]
        public string Ubicacion { get; set; }

        [Range(1, 20, ErrorMessage = "El número de salas debe estar entre 1 y 20.")]
        public int NumeroSalas { get; set; }

        // Coordenadas geográficas del cine, necesarias para ubicarlo en el mapa

        [Required(ErrorMessage = "La latitud es obligatoria.")]
        public double Latitud { get; set; }

        [Required(ErrorMessage = "La longitud es obligatoria.")]
        public double Longitud { get; set; }
    }
}