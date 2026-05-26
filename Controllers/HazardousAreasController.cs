using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SGA.Data;
using SGA.Models;

namespace SGA.Controllers
{
    [Authorize(Roles = "Administrator,Supervisor")]
    public class HazardousAreasController : Controller
    {
        private readonly SgaContext _context;

        public HazardousAreasController(SgaContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.HazardousAreas.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var hazardousArea = await _context.HazardousAreas.FirstOrDefaultAsync(m => m.HazardousAreaId == id);
            if (hazardousArea == null) return NotFound();

            return View(hazardousArea);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AreaKey,AreaName,AreaDescription,IsActive")] HazardousArea hazardousArea)
        {
            if (ModelState.IsValid)
            {
                hazardousArea.CreatedDate = DateTime.Now;
                _context.Add(hazardousArea);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(hazardousArea);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var hazardousArea = await _context.HazardousAreas.FindAsync(id);
            if (hazardousArea == null) return NotFound();

            return View(hazardousArea);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("HazardousAreaId,AreaKey,AreaName,AreaDescription,IsActive")] HazardousArea hazardousArea)
        {
            if (id != hazardousArea.HazardousAreaId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    hazardousArea.ModifiedDate = DateTime.Now;
                    _context.Update(hazardousArea);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HazardousAreaExists(hazardousArea.HazardousAreaId))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(hazardousArea);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var hazardousArea = await _context.HazardousAreas.FirstOrDefaultAsync(m => m.HazardousAreaId == id);
            if (hazardousArea == null) return NotFound();

            return View(hazardousArea);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hazardousArea = await _context.HazardousAreas.FindAsync(id);
            if (hazardousArea != null)
                _context.HazardousAreas.Remove(hazardousArea);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HazardousAreaExists(int id)
        {
            return _context.HazardousAreas.Any(e => e.HazardousAreaId == id);
        }
    }
}
