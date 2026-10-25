using System.ComponentModel.DataAnnotations;

namespace ProyectoCine.Models
{
    public class ReservaButaca
    {
        public int Id { get; set; }

        public int FuncionId { get; set; }

        [Required]
        [StringLength(20)]
        public string ButacaCodigo { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Pagada, Liberada

        public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
        public DateTime ExpiraEn { get; set; }
    }
}
