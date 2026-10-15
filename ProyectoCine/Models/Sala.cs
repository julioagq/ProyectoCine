
using System.ComponentModel.DataAnnotations;
// Permite usar anotaciones de esquema como [Column] y [ForeignKey]
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCine.Models
{
    public class Sala
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la sala es obligatorio.")]
        [StringLength(100)]
        [Display(Name = "Nombre / Número de Sala")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La capacidad es obligatoria.")]
        [Range(1, 500, ErrorMessage = "La capacidad debe estar entre 1 y 500 asientos.")]
        [Display(Name = "Capacidad (Asientos)")]
        public int Capacidad { get; set; }

        [Display(Name = "Cine")]
        public int CineId { get; set; }

        [ForeignKey("CineId")]
        public Cine? Cine { get; set; }

        
    }
}
