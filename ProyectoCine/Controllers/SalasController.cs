using Microsoft.AspNetCore.Mvc; // Controladores y vistas
using Microsoft.AspNetCore.Mvc.Rendering; // Para combos en formularios
using Microsoft.EntityFrameworkCore; // Acceso a la base de datos
using ProyectoCine.Data; // Contexto de datos
using ProyectoCine.Models; // Modelos del proyecto

public class SalasController : Controller
{
    private readonly ApplicationDbContext _context;

    // Constructor: recibe el contexto de la base de datos
    public SalasController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Lista todas las salas con su cine
    public async Task<IActionResult> Index()
    {
        var salas = _context.Salas.Include(s => s.Cine);
        return View(await salas.ToListAsync());
    }

    // Muestra detalles de una sala por id
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var sala = await _context.Salas
            .Include(s => s.Cine)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sala == null) return NotFound();

        return View(sala);
    }

    // Formulario para crear nueva sala
    public IActionResult Create()
    {
        ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre"); // Combo de cines
        return View();
    }

    // Guarda la nueva sala
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Nombre,Capacidad,CineId")] Sala sala)
    {
        if (ModelState.IsValid)
        {
            _context.Salas.Add(sala);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre", sala.CineId);
        return View(sala);
    }

    // Formulario para editar sala
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var sala = await _context.Salas.FindAsync(id);
        if (sala == null) return NotFound();

        ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre", sala.CineId);
        return View(sala);
    }

    // Guarda cambios de la sala
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Capacidad,CineId")] Sala datosSala)
    {
        if (id != datosSala.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var sala = await _context.Salas.FindAsync(id);
            if (sala == null) return NotFound();

            // Actualiza propiedades
            sala.Nombre = datosSala.Nombre;
            sala.Capacidad = datosSala.Capacidad;
            sala.CineId = datosSala.CineId;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre", datosSala.CineId);
        return View(datosSala);
    }

    // Confirmación para borrar sala
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var sala = await _context.Salas
            .Include(s => s.Cine)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sala == null) return NotFound();

        return View(sala);
    }

    // Elimina la sala
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sala = await _context.Salas.FindAsync(id);
        if (sala != null)
        {
            _context.Salas.Remove(sala);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    // Nota: manejo de imágenes eliminado, no se guardan imágenes por sala
}
