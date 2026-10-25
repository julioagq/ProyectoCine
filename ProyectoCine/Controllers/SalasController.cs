using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoCine.Data;
using ProyectoCine.Models;

namespace ProyectoCine.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class SalasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Salas
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Salas.Include(s => s.Cine);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Salas/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var sala = await _context.Salas
                .Include(s => s.Cine)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (sala == null) return NotFound();

            return View(sala);
        }

        // GET: Salas/Create
        public IActionResult Create()
        {
            ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre");
            return View();
        }

        // POST: Salas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Capacidad,CineId")] Sala sala)
        {
            if (ModelState.IsValid)
            {
                _context.Add(sala);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre", sala.CineId);
            return View(sala);
        }

        // GET: Salas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var sala = await _context.Salas.FindAsync(id);
            if (sala == null) return NotFound();
            ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre", sala.CineId);
            return View(sala);
        }

        // POST: Salas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Capacidad,CineId")] Sala sala)
        {
            if (id != sala.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(sala);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SalaExists(sala.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CineId"] = new SelectList(_context.Cines, "Id", "Nombre", sala.CineId);
            return View(sala);
        }

        // GET: Salas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var sala = await _context.Salas
                .Include(s => s.Cine)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (sala == null) return NotFound();

            return View(sala);
        }

        // POST: Salas/Delete/5 [ELIMINACIÓN EN CASCADA PARA BORRAR FUNCIONES Y SALA]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sala = await _context.Salas.FindAsync(id);
            if (sala != null)
            {
                // 1. Elimina primero las funciones asociadas a esta sala para evitar errores de llave foránea[cite: 8]
                var funcionesAsociadas = _context.Funciones.Where(f => f.SalaId == id);
                _context.Funciones.RemoveRange(funcionesAsociadas);

                // 2. Elimina la sala exitosamente
                _context.Salas.Remove(sala);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SalaExists(int id)
        {
            return _context.Salas.Any(e => e.Id == id);
        }
    }
}