using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SGA.Data;
using SGA.Models;

namespace SGA.Controllers
{
    [Authorize(Roles = "Administrator,Supervisor")]
    public class PartNumbersController : Controller
    {
        private readonly SgaContext _context;

        public PartNumbersController(SgaContext context)
        {
            _context = context;
        }

        // GET: PartNumbers
        public async Task<IActionResult> Index()
        {
            return View(await _context.PartNumbers.ToListAsync());
        }

        // GET: PartNumbers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partNumber = await _context.PartNumbers
                .FirstOrDefaultAsync(m => m.PartNumberId == id);
            if (partNumber == null)
            {
                return NotFound();
            }

            return View(partNumber);
        }

        // GET: PartNumbers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PartNumbers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PartNumberId,PartNumberKey,PartNumberName,PartNumberNameGdi,PartNumber1,PartNumberProgram")] PartNumber partNumber)
        {
            if (ModelState.IsValid)
            {
                _context.Add(partNumber);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(partNumber);
        }

        // GET: PartNumbers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partNumber = await _context.PartNumbers.FindAsync(id);
            if (partNumber == null)
            {
                return NotFound();
            }
            return View(partNumber);
        }

        // POST: PartNumbers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PartNumberId,PartNumberKey,PartNumberName,PartNumberNameGdi,PartNumber1,PartNumberProgram")] PartNumber partNumber)
        {
            if (id != partNumber.PartNumberId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(partNumber);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PartNumberExists(partNumber.PartNumberId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(partNumber);
        }

        // GET: PartNumbers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partNumber = await _context.PartNumbers
                .FirstOrDefaultAsync(m => m.PartNumberId == id);
            if (partNumber == null)
            {
                return NotFound();
            }

            return View(partNumber);
        }

        // POST: PartNumbers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var partNumber = await _context.PartNumbers.FindAsync(id);
            if (partNumber != null)
            {
                _context.PartNumbers.Remove(partNumber);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PartNumberExists(int id)
        {
            return _context.PartNumbers.Any(e => e.PartNumberId == id);
        }
    }
}
