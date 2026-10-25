using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; // Controladores y vistas
using Microsoft.AspNetCore.Mvc.Rendering; // Para combos en formularios
using Microsoft.EntityFrameworkCore; // Acceso a la base de datos
using ProyectoCine.Data; // Contexto de datos
using ProyectoCine.Models; // Modelos del proyecto
using System.Security.Claims;

namespace ProyectoCine.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class FuncionesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FuncionesController(ApplicationDbContext context) // Recibe el contexto
        {
            _context = context;
        }

        // Lista todas las funciones con su película, sala y cine
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var funciones = _context.Funciones
                .Include(f => f.Pelicula) // Trae la película
                .Include(f => f.Sala)     // Trae la sala
                    .ThenInclude(s => s.Cine); // Trae el cine de la sala

            return View(await funciones.ToListAsync());
        }

        // Muestra detalles de una función por id
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var funcion = await _context.Funciones
                .Include(f => f.Pelicula)
                .Include(f => f.Sala)
                    .ThenInclude(s => s.Cine)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (funcion == null) return NotFound();

            var ahora = DateTime.UtcNow;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var reservasVigentes = await _context.ReservasButacas
                .Where(r => r.FuncionId == funcion.Id &&
                            (r.Estado == "Pagada" || (r.Estado == "Pendiente" && r.ExpiraEn > ahora)))
                .Select(r => new { r.ButacaCodigo, r.UserId })
                .ToListAsync();

            var butacasPropias = reservasVigentes
                .Where(r => !string.IsNullOrEmpty(userId) && r.UserId == userId)
                .Select(r => r.ButacaCodigo)
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            var butacasOtros = reservasVigentes
                .Where(r => string.IsNullOrEmpty(userId) || r.UserId != userId)
                .Select(r => r.ButacaCodigo)
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            ViewBag.ButacasReservadasPropias = butacasPropias;
            ViewBag.ButacasReservadasOtros = butacasOtros;
            return View(funcion);
        }

        // Formulario para crear nueva función
        public IActionResult Create()
        {
            CargarSelectLists(); // Prepara combos
            return View();
        }

        // Guarda la nueva función
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,PeliculaId,SalaId,FechaHora,Precio")] Funcion funcion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(funcion); // Agrega
                await _context.SaveChangesAsync(); // Guarda
                return RedirectToAction(nameof(Index));
            }

            CargarSelectLists(funcion.PeliculaId, funcion.SalaId); // Recarga combos si hay error
            return View(funcion);
        }

        // Formulario para editar función
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var funcion = await _context.Funciones.FindAsync(id);
            if (funcion == null) return NotFound();

            CargarSelectLists(funcion.PeliculaId, funcion.SalaId);
            return View(funcion);
        }

        // Guarda cambios de la edición
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PeliculaId,SalaId,FechaHora,Precio")] Funcion funcion)
        {
            if (id != funcion.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(funcion); // Actualiza
                    await _context.SaveChangesAsync(); // Guarda cambios
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FuncionExists(funcion.Id)) return NotFound(); // Si no existe
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            CargarSelectLists(funcion.PeliculaId, funcion.SalaId);
            return View(funcion);
        }

        // Confirmación para borrar función
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var funcion = await _context.Funciones
                .Include(f => f.Pelicula)
                .Include(f => f.Sala)
                    .ThenInclude(s => s.Cine)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (funcion == null) return NotFound();

            return View(funcion);
        }

        // Elimina la función
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var affectedRows = await _context.Funciones
                .Where(f => f.Id == id)
                .ExecuteDeleteAsync();

            if (affectedRows == 0) return NotFound();

            return RedirectToAction(nameof(Index));
        }

        // Revisa si existe una función por id
        private bool FuncionExists(int id)
        {
            return _context.Funciones.Any(e => e.Id == id);
        }

        // Prepara combos de películas y salas
        private void CargarSelectLists(int? peliculaId = null, int? salaId = null)
        {
            ViewData["PeliculaId"] = new SelectList(_context.Peliculas, "Id", "Titulo", peliculaId);

            var salas = _context.Salas.Include(s => s.Cine)
                .Select(s => new {
                    Id = s.Id,
                    Nombre = s.Cine != null ? $"{s.Nombre} ({s.Cine.Nombre})" : s.Nombre
                });

            ViewData["SalaId"] = new SelectList(salas, "Id", "Nombre", salaId);
        }
    }
}
