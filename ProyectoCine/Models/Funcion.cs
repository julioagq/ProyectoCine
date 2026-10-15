using System.ComponentModel.DataAnnotations;
// Permite usar anotaciones de esquema como [Column] y [ForeignKey]
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCine.Models
{
    public class Funcion
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una película.")]
        [Display(Name = "Película")]
        public int PeliculaId { get; set; }

        [ForeignKey("PeliculaId")]
        public Pelicula? Pelicula { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una sala.")]
        [Display(Name = "Sala")]
        public int SalaId { get; set; }

        [ForeignKey("SalaId")]
        public Sala? Sala { get; set; }

        [Required(ErrorMessage = "La fecha y hora son obligatorias.")]
        [Display(Name = "Fecha y Hora")]
        [DataType(DataType.DateTime)]
        public DateTime FechaHora { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El precio de la entrada es obligatorio.")]
        [Range(1000, 20000, ErrorMessage = "El precio debe estar entre 1,000 y 20,000 colones.")]
        [Display(Name = "Precio Entrada (₡)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }
    }
}