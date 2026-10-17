using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCine.Data;
using ProyectoCine.Models;

namespace ProyectoCine.Controllers
{
    public class CinesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CinesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Validación estricta y absoluta: ÚNICAMENTE admin@cine.com
        private bool EsAdministrador()
        {
            return User?.Identity?.IsAuthenticated == true &&
                   User.Identity.Name != null &&
                   User.Identity.Name.Trim().ToLower() == "admin@cine.com";
        }

        // GET: Cines (Público)
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Cines.ToListAsync());
        }

        // GET: Cines/Details/5 (Público)
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cine = await _context.Cines.FirstOrDefaultAsync(m => m.Id == id);
            if (cine == null) return NotFound();

            return View(cine);
        }

        // GET: Cines/Create (SOLO ADMIN)
        public IActionResult Create()
        {
            if (!EsAdministrador()) return RedirectToAction(nameof(Index));
            return View();
        }

        // POST: Cines/Create (SOLO ADMIN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Ubicacion,NumeroSalas,Latitud,Longitud")] Cine cine)
        {
            if (!EsAdministrador()) return RedirectToAction(nameof(Index));

            if (ModelState.IsValid)
            {
                _context.Add(cine);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cine);
        }

        // GET: Cines/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!EsAdministrador()) return RedirectToAction(nameof(Index));

            if (id == null) return NotFound();

            var cine = await _context.Cines.FindAsync(id);
            if (cine == null) return NotFound();

            return View(cine);
        }

        // POST: Cines/Edit/5 -> ACTUALIZACIÓN BLINDADA Y DIRECTA A LA BASE DE DATOS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Ubicacion,NumeroSalas,Latitud,Longitud")] Cine cine)
        {
            if (!EsAdministrador())
            {
                return RedirectToAction(nameof(Index));
            }

            if (id != cine.Id)
            {
                return NotFound();
            }

            // Parseo seguro de coordenadas con soporte universal de puntos y comas
            var latStr = Request.Form["Latitud"].ToString().Replace(',', '.');
            var lngStr = Request.Form["Longitud"].ToString().Replace(',', '.');

            if (double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
            {
                cine.Latitud = lat;
            }
            if (double.TryParse(lngStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lng))
            {
                cine.Longitud = lng;
            }

            try
            {
                // Forzamos la actualización directa en la base de datos sin depender de estados frágiles del ModelState
                _context.Update(cine);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CineExists(cine.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar los cambios: " + ex.Message);
            }

            return View(cine);
        }

        // GET: Cines/Delete/5 (SOLO ADMIN)
        public async Task<IActionResult> Delete(int? id)
        {
            if (!EsAdministrador()) return RedirectToAction(nameof(Index));

            if (id == null) return NotFound();

            var cine = await _context.Cines.FirstOrDefaultAsync(m => m.Id == id);
            if (cine == null) return NotFound();

            return View(cine);
        }

        // POST: Cines/Delete/5 (SOLO ADMIN)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!EsAdministrador()) return RedirectToAction(nameof(Index));

            var cine = await _context.Cines.FindAsync(id);
            if (cine != null)
            {
                try
                {
                    _context.Cines.Remove(cine);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "No se puede eliminar este cine porque tiene salas o funciones asociadas.");
                    return View(cine);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CineExists(int id)
        {
            return _context.Cines.Any(e => e.Id == id);
        }
    }
}