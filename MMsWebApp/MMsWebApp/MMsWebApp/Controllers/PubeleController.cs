using Microsoft.AspNetCore.Mvc;
using MMsWebApp.Data;
using MMsWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MMsWebApp.Controllers
{
    public class PubeleController : Controller
    {
        private readonly AppDbContext _context;

        public PubeleController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new Pubela
            {
                Id = string.Empty,
                Tip = string.Empty
            };

            ViewBag.Pubele = await _context.Pubele.ToListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pubela pubela)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Pubele.Add(pubela);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Nu s-a putut salva pubela. Verificați datele și încercați din nou.");
                }
            }

            ViewBag.Pubele = await _context.Pubele.ToListAsync();
            return View("Index", pubela);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAjax(string id, [FromBody] Pubela pubela)
        {
            if (id != pubela.Id)
            {
                return Json(new { success = false, message = "ID invalid" });
            }

            try
            {
                var existing = await _context.Pubele.FindAsync(id);
                if (existing == null)
                {
                    return Json(new { success = false, message = "Pubela nu a fost găsită" });
                }

                existing.Tip = pubela.Tip;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var pubela = await _context.Pubele.FindAsync(id);
            if (pubela == null)
            {
                return NotFound();
            }

            try
            {
                _context.Pubele.Remove(pubela);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "Nu se poate șterge pubela deoarece are asocieri sau colectări.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}