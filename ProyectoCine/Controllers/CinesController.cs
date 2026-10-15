using Microsoft.AspNetCore.Mvc; // Controladores y vistas MVC
using Microsoft.EntityFrameworkCore; // Acceso a base de datos con EF Core
using ProyectoCine.Models; // Modelos del proyecto (ej. Cine)
using ProyectoCine.Data; // Contexto de datos (ApplicationDbContext)
using System.Globalization; // Manejo de formatos culturales (lat/long)

public class CinesController : Controller
{
    private readonly ApplicationDbContext _context;

    // Constructor: recibe el contexto de la base de datos
    public CinesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Acción Index: lista todos los cines
    public async Task<IActionResult> Index()
    {
        return View(await _context.Cines.ToListAsync());
    }

    // Acción Details: muestra detalles de un cine por id
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var cine = await _context.Cines.FirstOrDefaultAsync(m => m.Id == id);
        if (cine == null) return NotFound();
        return View(cine);
    }

    // Acción Create (GET): muestra formulario de creación
    public IActionResult Create()
    {
        return View();
    }

    // Acción Create (POST): guarda un nuevo cine
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Nombre,Ubicacion,NumeroSalas,Latitud,Longitud")] Cine cine)
    {
        // Normaliza latitud/longitud con punto decimal
        string latStr = Request.Form["Latitud"].ToString().Replace(',', '.');
        string lngStr = Request.Form["Longitud"].ToString().Replace(',', '.');

        if (double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double latVal))
            cine.Latitud = latVal;
        if (double.TryParse(lngStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lngVal))
            cine.Longitud = lngVal;

        // Remueve validación automática de lat/long
        ModelState.Remove(nameof(Cine.Latitud));
        ModelState.Remove(nameof(Cine.Longitud));

        if (ModelState.IsValid)
        {
            _context.Add(cine);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(cine);
    }

    // Acción Edit (GET): muestra formulario de edición
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var cine = await _context.Cines.FindAsync(id);
        if (cine == null) return NotFound();
        return View(cine);
    }

    // Acción Edit (POST): actualiza un cine existente
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,Nombre,Ubicacion,NumeroSalas,Latitud,Longitud")] Cine cine)
    {
        if (id != cine.Id) return NotFound();

        // Normaliza latitud/longitud
        string latStr = Request.Form["Latitud"].ToString().Replace(',', '.');
        string lngStr = Request.Form["Longitud"].ToString().Replace(',', '.');

        if (double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double latVal))
            cine.Latitud = latVal;
        if (double.TryParse(lngStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lngVal))
            cine.Longitud = lngVal;

        ModelState.Remove(nameof(Cine.Latitud));
        ModelState.Remove(nameof(Cine.Longitud));

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(cine);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CineExists(cine.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(cine);
    }

    // Acción Delete (GET): muestra confirmación de eliminación
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var cine = await _context.Cines.FirstOrDefaultAsync(m => m.Id == id);
        if (cine == null) return NotFound();
        return View(cine);
    }

    // Acción Delete (POST): elimina un cine
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var cine = await _context.Cines.FindAsync(id);
        if (cine != null) _context.Cines.Remove(cine);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Método auxiliar: verifica si existe un cine por id
    private bool CineExists(int? id)
    {
        return _context.Cines.Any(e => e.Id == id);
    }
}
