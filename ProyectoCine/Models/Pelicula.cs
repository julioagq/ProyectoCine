using System.ComponentModel.DataAnnotations;
// Permite usar anotaciones de esquema como [Column] y [ForeignKey]
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCine.Models
{
    public class Pelicula
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(150)]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El género es obligatorio.")]
        [StringLength(50)]
        [Display(Name = "Género")]
        public string Genero { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Duración (minutos)")]
        public int DuracionMinutos { get; set; }

        [Required]
        [Display(Name = "Clasificación")]
        public string Clasificacion { get; set; } = string.Empty;

        [Display(Name = "Sinopsis")]
        public string? Sinopsis { get; set; }

        [Display(Name = "Imagen de la película")]
        public string? ImagenUrl { get; set; }

        // Campo Precio con validación de rango y formato
        [Range(0, 100000, ErrorMessage = "El precio debe ser un valor no negativo.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Precio de la entrada")]
        public decimal Precio { get; set; }


        [Display(Name = "Cine")]
        public int CineId { get; set; }

        [ForeignKey("CineId")]
        public Cine? Cine { get; set; }
    }
}