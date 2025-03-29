using Microsoft.AspNetCore.Mvc;
using MMsWebApp.Data;
using MMsWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MMsWebApp.Controllers
{
    public class PubeleCetateniController : Controller
    {
        private readonly AppDbContext _context;

        public PubeleCetateniController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new PubelaCetatean
            {
                PubelaId = string.Empty,
                CetateanId = 0,
                Adresa = string.Empty
            };

            
            ViewBag.PubeleCetateni = await _context.PubeleCetateni
                .Include(pc => pc.Pubela)
                .Include(pc => pc.Cetatean)
                .ToListAsync();
            ViewBag.Pubele = await _context.Pubele.ToListAsync();
            ViewBag.Cetateni = await _context.Cetateni.ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PubelaCetatean pubelaCetatean)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.PubeleCetateni.Add(pubelaCetatean);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Nu s-a putut salva asocierea. Verificați datele și încercați din nou.");
                }
            }

            ViewBag.PubeleCetateni = await _context.PubeleCetateni
                .Include(pc => pc.Pubela)
                .Include(pc => pc.Cetatean)
                .ToListAsync();
            ViewBag.Pubele = await _context.Pubele.ToListAsync();
            ViewBag.Cetateni = await _context.Cetateni.ToListAsync();
            return View("Index", pubelaCetatean);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAjax(int id, [FromBody] PubelaCetatean pubelaCetatean)
        {
            if (id != pubelaCetatean.Id)
            {
                return Json(new { success = false, message = "ID invalid" });
            }

            try
            {
                var existing = await _context.PubeleCetateni.FindAsync(id);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Asocierea nu a fost găsită" });
                }

                existing.PubelaId = pubelaCetatean.PubelaId;
                existing.CetateanId = pubelaCetatean.CetateanId;
                existing.Adresa = pubelaCetatean.Adresa;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var pubelaCetatean = await _context.PubeleCetateni.FindAsync(id);
            if (pubelaCetatean == null)
            {
                return NotFound();
            }

            try
            {
                _context.PubeleCetateni.Remove(pubelaCetatean);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "Nu se poate șterge asocierea.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}