using Microsoft.AspNetCore.Identity;

namespace ProyectoCine.Models
{
    public class PerfilViewModel
    {
        public IdentityUser Usuario { get; set; } = null!;
        public bool EsAdmin { get; set; }
        public List<Orden> Ordenes { get; set; } = new();
        public List<ReservaButaca> ReservasPendientes { get; set; } = new();
        public List<ReservaButaca> ReservasPagadas { get; set; } = new();
        public Dictionary<string, string> UsuariosPorId { get; set; } = new();
    }
}
