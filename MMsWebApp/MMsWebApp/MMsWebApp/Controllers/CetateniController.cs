using Microsoft.AspNetCore.Mvc;
using MMsWebApp.Data;
using MMsWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MMsWebApp.Controllers
{
    public class CetateniController : Controller
    {
        private readonly AppDbContext _context;

        public CetateniController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new Cetatean
            {
                Nume = string.Empty,
                Prenume = string.Empty,
                Email = string.Empty,
                CNP = string.Empty
            };

            ViewBag.Cetateni = await _context.Cetateni.ToListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cetatean cetatean)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Cetateni.Add(cetatean);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Nu s-a putut salva cetățeanul. Verificați datele și încercați din nou.");
                }
            }
            ViewBag.Cetateni = await _context.Cetateni.ToListAsync();
            return View("Index", cetatean);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAjax(int id, [FromBody] Cetatean cetatean)
        {
            if (id != cetatean.Id)
            {
                return Json(new { success = false, message = "ID invalid" });
            }

            try
            {
                var existingCetatean = await _context.Cetateni.FindAsync(id);
                if (existingCetatean == null)
                {
                    return Json(new { success = false, message = "Cetățeanul nu a fost găsit" });
                }

                existingCetatean.Nume = cetatean.Nume;
                existingCetatean.Prenume = cetatean.Prenume;
                existingCetatean.Email = cetatean.Email;
                existingCetatean.CNP = cetatean.CNP;

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
            var cetatean = await _context.Cetateni.FindAsync(id);
            if (cetatean == null)
            {
                return NotFound();
            }

            try
            {
                _context.Cetateni.Remove(cetatean);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "Nu se poate șterge cetățeanul deoarece are pubele asociate.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
