using Microsoft.AspNetCore.Mvc; // Controladores y vistas
using Microsoft.AspNetCore.Mvc.Rendering; // Para listas desplegables
using Microsoft.EntityFrameworkCore; // Acceso a la base de datos
using ProyectoCine.Data; // Contexto de datos
using ProyectoCine.Models; // Modelos del proyecto

public class PeliculasController : Controller
{
    private readonly ApplicationDbContext _context;

    public PeliculasController(ApplicationDbContext context) // Constructor recibe el contexto
    {
        _context = context;
    }

    // Lista todas las películas con su cine
    public async Task<IActionResult> Index()
    {
        var peliculas = _context.Peliculas.Include(p => p.Cine);
        return View(await peliculas.ToListAsync());
    }

    // Muestra detalles de una película por id
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var pelicula = await _context.Peliculas
            .Include(p => p.Cine)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (pelicula == null) return NotFound();

        return View(pelicula);
    }

    // Formulario para crear nueva película
    public IActionResult Create()
    {
        ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre"); // Combo de cines
        return View();
    }

    // Guarda la nueva película
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Titulo,Genero,DuracionMinutos,Clasificacion,Sinopsis,ImagenUrl,Precio,CineId")] Pelicula pelicula)
    {
        if (ModelState.IsValid)
        {
            _context.Add(pelicula);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre", pelicula.CineId);
        return View(pelicula);
    }

    // Formulario para editar película
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var pelicula = await _context.Peliculas.FindAsync(id);
        if (pelicula == null) return NotFound();

        ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre", pelicula.CineId);
        return View(pelicula);
    }

    // Guarda cambios de la edición
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Titulo,Genero,DuracionMinutos,Clasificacion,Sinopsis,ImagenUrl,Precio,CineId")] Pelicula pelicula)
    {
        if (id != pelicula.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(pelicula);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PeliculaExists(pelicula.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre", pelicula.CineId);
        return View(pelicula);
    }

    // Confirmación para borrar película
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var pelicula = await _context.Peliculas
            .Include(p => p.Cine)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (pelicula == null) return NotFound();

        return View(pelicula);
    }

    // Elimina la película
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var pelicula = await _context.Peliculas.FindAsync(id);
        if (pelicula != null) _context.Peliculas.Remove(pelicula);

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Revisa si existe una película por id
    private bool PeliculaExists(int id)
    {
        return _context.Peliculas.Any(e => e.Id == id);
    }

    // Genera sinopsis automática con IA
    [HttpGet]
    public IActionResult GenerarSinopsisIA(string titulo, string genero)
    {
        if (string.IsNullOrEmpty(titulo))
        {
            return Content("Por favor ingresa un título para generar la sinopsis.");
        }

        string sinopsisGenerada = $"'{titulo}' es una fascinante producción cinematográfica del género {genero ?? "drama"}. " +
                                 $"Sigue una historia cautivadora llena de giros inesperados, momentos emotivos y un desarrollo de personajes excepcional " +
                                 $"que mantendrá a la audiencia al borde de su asiento de principio a fin.";

        return Content(sinopsisGenerada);
    }

    // Genera URL de imagen (poster) automática
    [HttpGet]
    public IActionResult GenerarImagenUrlIA(string titulo, string genero)
    {
        if (string.IsNullOrEmpty(titulo))
        {
            return Content("https://via.placeholder.com/500x750?text=Sin+Imagen");
        }

        string query = Uri.EscapeDataString($"{titulo} movie poster {genero}".Trim());
        string imagenUrl = $"https://image.pollinations.ai/prompt/{query}?width=500&height=750&nologo=true";

        return Content(imagenUrl);
    }
}
