using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SGA.Data;
using SGA.Models;

namespace SGA.Controllers
{
    [Authorize(Roles = "Administrator,Supervisor")]
    public class HazardousWasteController : Controller
    {
        private readonly SgaContext _context;

        public HazardousWasteController(SgaContext context)
        {
            _context = context;
        }

        // GET: HazardousWaste
        public async Task<IActionResult> Index()
        {
            var wastes = await _context.HazardousWastes
                .Include(w => w.HazardousWasteCretibs)
                    .ThenInclude(wc => wc.Cretib)
                .OrderByDescending(w => w.CreatedDate)
                .ToListAsync();

            return View(wastes);
        }

        // GET: HazardousWaste/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.CretibList = _context.Cretibs.OrderBy(c => c.CretibKey).ToList();
            return View();
        }

        // POST: HazardousWaste/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HazardousWaste model, List<int> selectedCretib)
        {
            if (string.IsNullOrWhiteSpace(model.WasteKey))
                ModelState.AddModelError("WasteKey", "La clave es obligatoria.");

            if (string.IsNullOrWhiteSpace(model.WasteName))
                ModelState.AddModelError("WasteName", "El nombre es obligatorio.");

            // Verificar si ya existe la clave
            var existingWaste = await _context.HazardousWastes
                .FirstOrDefaultAsync(w => w.WasteKey == model.WasteKey.Trim());

            if (existingWaste != null)
                ModelState.AddModelError("WasteKey", "Ya existe un residuo con esta clave.");

            if (ModelState.IsValid)
            {
                model.WasteKey = model.WasteKey.Trim().ToUpper();
                model.WasteName = model.WasteName.Trim();
                model.CreatedDate = DateTime.Now;
                model.IsActive = true;

                _context.HazardousWastes.Add(model);
                await _context.SaveChangesAsync();

                // Agregar códigos CRETIB seleccionados
                if (selectedCretib != null && selectedCretib.Any())
                {
                    foreach (var cretibId in selectedCretib)
                    {
                        var wasteCretib = new HazardousWasteCretib
                        {
                            HazardousWasteId = model.HazardousWasteId,
                            CretibId = cretibId,
                            CreatedDate = DateTime.Now
                        };
                        _context.HazardousWasteCretibs.Add(wasteCretib);
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Residuo peligroso creado correctamente";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CretibList = _context.Cretibs.OrderBy(c => c.CretibKey).ToList();
            return View(model);
        }

        // GET: HazardousWaste/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var waste = await _context.HazardousWastes
                .Include(w => w.HazardousWasteCretibs)
                .FirstOrDefaultAsync(w => w.HazardousWasteId == id);

            if (waste == null)
                return NotFound();

            ViewBag.CretibList = _context.Cretibs.OrderBy(c => c.CretibKey).ToList();
            ViewBag.SelectedCretib = waste.HazardousWasteCretibs.Select(wc => wc.CretibId).ToList();

            return View(waste);
        }

        // POST: HazardousWaste/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HazardousWaste model, List<int> selectedCretib)
        {
            if (id != model.HazardousWasteId)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.WasteKey))
                ModelState.AddModelError("WasteKey", "La clave es obligatoria.");

            if (string.IsNullOrWhiteSpace(model.WasteName))
                ModelState.AddModelError("WasteName", "El nombre es obligatorio.");

            // Verificar si ya existe otra clave igual
            var existingWaste = await _context.HazardousWastes
                .FirstOrDefaultAsync(w => w.WasteKey == model.WasteKey.Trim() && w.HazardousWasteId != id);

            if (existingWaste != null)
                ModelState.AddModelError("WasteKey", "Ya existe otro residuo con esta clave.");

            if (ModelState.IsValid)
            {
                var dbWaste = await _context.HazardousWastes.FindAsync(id);
                if (dbWaste == null)
                    return NotFound();

                dbWaste.WasteKey = model.WasteKey.Trim().ToUpper();
                dbWaste.WasteName = model.WasteName.Trim();
                dbWaste.WasteDescription = model.WasteDescription?.Trim();
                dbWaste.IsActive = model.IsActive;
                dbWaste.ModifiedDate = DateTime.Now;

                // Eliminar códigos CRETIB anteriores
                var existingCretibs = _context.HazardousWasteCretibs
                    .Where(wc => wc.HazardousWasteId == id);
                _context.HazardousWasteCretibs.RemoveRange(existingCretibs);

                // Agregar códigos CRETIB seleccionados
                if (selectedCretib != null && selectedCretib.Any())
                {
                    foreach (var cretibId in selectedCretib)
                    {
                        var wasteCretib = new HazardousWasteCretib
                        {
                            HazardousWasteId = id,
                            CretibId = cretibId,
                            CreatedDate = DateTime.Now
                        };
                        _context.HazardousWasteCretibs.Add(wasteCretib);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Residuo peligroso actualizado correctamente";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CretibList = _context.Cretibs.OrderBy(c => c.CretibKey).ToList();
            ViewBag.SelectedCretib = selectedCretib ?? new List<int>();

            return View(model);
        }

        // POST: HazardousWaste/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var waste = await _context.HazardousWastes
                    .Include(w => w.HazardousWasteCretibs)
                    .FirstOrDefaultAsync(w => w.HazardousWasteId == id);

                if (waste == null)
                    return Json(new { success = false, message = "Residuo no encontrado." });

                // Eliminar relaciones CRETIB
                _context.HazardousWasteCretibs.RemoveRange(waste.HazardousWasteCretibs);

                // Eliminar residuo
                _context.HazardousWastes.Remove(waste);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: HazardousWaste/ToggleActive/5
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var waste = await _context.HazardousWastes.FindAsync(id);
                if (waste == null)
                    return Json(new { success = false, message = "Residuo no encontrado." });

                waste.IsActive = !waste.IsActive;
                waste.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new { success = true, isActive = waste.IsActive });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
