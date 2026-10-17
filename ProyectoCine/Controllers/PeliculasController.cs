using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoCine.Data;
using ProyectoCine.Models;

namespace ProyectoCine.Controllers
{
    public class PeliculasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public PeliculasController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Peliculas
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Peliculas.Include(p => p.Cine);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Peliculas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pelicula = await _context.Peliculas
                .Include(p => p.Cine)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pelicula == null) return NotFound();

            return View(pelicula);
        }

        // GET: Peliculas/Create
        [Authorize]
        public IActionResult Create()
        {
            ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre");
            return View();
        }

        // POST: Peliculas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Id,Titulo,Genero,DuracionMinutos,Clasificacion,Sinopsis,ImagenUrl,ImagenArchivo,Precio,CineId")] Pelicula pelicula)
        {
            if (ModelState.IsValid)
            {
                // Procesar la subida del archivo de imagen local
                if (pelicula.ImagenArchivo != null && pelicula.ImagenArchivo.Length > 0)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(pelicula.ImagenArchivo.FileName);
                    string peliculasPath = Path.Combine(wwwRootPath, "images", "peliculas");

                    if (!Directory.Exists(peliculasPath))
                    {
                        Directory.CreateDirectory(peliculasPath);
                    }

                    string filePath = Path.Combine(peliculasPath, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await pelicula.ImagenArchivo.CopyToAsync(fileStream);
                    }

                    pelicula.ImagenUrl = "/images/peliculas/" + fileName;
                }

                _context.Add(pelicula);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre", pelicula.CineId);
            return View(pelicula);
        }

        // GET: Peliculas/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pelicula = await _context.Peliculas.FindAsync(id);
            if (pelicula == null) return NotFound();

            ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre", pelicula.CineId);
            return View(pelicula);
        }

        // POST: Peliculas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titulo,Genero,DuracionMinutos,Clasificacion,Sinopsis,ImagenUrl,ImagenArchivo,Precio,CineId")] Pelicula pelicula)
        {
            if (id != pelicula.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Procesar la subida de nueva imagen o conservar la anterior
                    if (pelicula.ImagenArchivo != null && pelicula.ImagenArchivo.Length > 0)
                    {
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(pelicula.ImagenArchivo.FileName);
                        string peliculasPath = Path.Combine(wwwRootPath, "images", "peliculas");

                        if (!Directory.Exists(peliculasPath))
                        {
                            Directory.CreateDirectory(peliculasPath);
                        }

                        string filePath = Path.Combine(peliculasPath, fileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await pelicula.ImagenArchivo.CopyToAsync(fileStream);
                        }

                        pelicula.ImagenUrl = "/images/peliculas/" + fileName;
                    }
                    else
                    {
                        var peliculaDb = await _context.Peliculas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                        if (peliculaDb != null)
                        {
                            pelicula.ImagenUrl = peliculaDb.ImagenUrl;
                        }
                    }

                    _context.Update(pelicula);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Peliculas.Any(e => e.Id == pelicula.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre", pelicula.CineId);
            return View(pelicula);
        }

        // GET: Peliculas/Delete/5 (Muestra primero la interfaz de confirmación)
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var pelicula = await _context.Peliculas
                .Include(p => p.Cine)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pelicula == null) return NotFound();

            return View(pelicula);
        }

        // POST: Peliculas/Delete/5 (Ejecuta el borrado definitivo tras confirmar)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pelicula = await _context.Peliculas.FindAsync(id);

            if (pelicula != null)
            {
                try
                {
                    _context.Peliculas.Remove(pelicula);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Si la base de datos rechaza el borrado por tener registros vinculados, 
                    // capturamos el error y se lo avisamos al usuario de forma limpia en la interfaz
                    TempData["ErrorDelete"] = "No se puede eliminar esta película porque tiene registros o funciones asociadas en el sistema.";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // Acción auxiliar para generar sinopsis por IA
        public async Task<IActionResult> GenerarSinopsisIA(string titulo, string genero)
        {
            await Task.CompletedTask;
            return Content($"Sinopsis generada automáticamente para la película {titulo} del género {genero}.");
        }
    }
}