using Microsoft.AspNetCore.Mvc;
using MMsWebApp.Data;
using MMsWebApp.Models;

namespace MMsWebApp.Controllers
{
    public class ColectariController : Controller
    {
        private readonly AppDbContext _context;

        public ColectariController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new Colectare
            {
                IdPubela = string.Empty,
                CollectedAt = DateTime.Now
            };
            
            ViewBag.Pubele = _context.Pubele.ToList();
            ViewBag.Colectari = _context.Colectari.ToList();
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Colectare colectare)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Colectari.Add(colectare);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Nu s-a putut salva colectarea. Verificați datele și încercați din nou.");
                }
            }
            
            ViewBag.Pubele = _context.Pubele.ToList();
            ViewBag.Colectari = _context.Colectari.ToList();
            return View("Index", colectare);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var colectare = await _context.Colectari.FindAsync(id);
            if (colectare == null)
            {
                return NotFound();
            }

            _context.Colectari.Remove(colectare);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var colectare = await _context.Colectari.FindAsync(id);
            if (colectare == null)
            {
                return NotFound();
            }

            ViewBag.Pubele = _context.Pubele.ToList();
            return View(colectare);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Colectare colectare)
        {
            if (id != colectare.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(colectare);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "A apărut o eroare la salvare. Verificați datele și încercați din nou.");
                }
            }

            ViewBag.Pubele = _context.Pubele.ToList();
            return View(colectare);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAjax(int id, [FromBody] Colectare colectare)
        {
            if (id != colectare.Id)
            {
                return Json(new { success = false, message = "ID invalid" });
            }

            try
            {
                var existingColectare = await _context.Colectari.FindAsync(id);
                if (existingColectare == null)
                {
                    return Json(new { success = false, message = "Colectarea nu a fost găsită" });
                }

                existingColectare.IdPubela = colectare.IdPubela;
                existingColectare.CollectedAt = colectare.CollectedAt;

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult ColectariCetatean(int cetateanId)
        {
            var colectari = _context.Colectari
                .Where(c => _context.PubeleCetateni
                    .Any(pc => pc.CetateanId == cetateanId && pc.PubelaId == c.IdPubela))
                .ToList();

            var cetatean = _context.Cetateni.Find(cetateanId);
            if (cetatean == null)
            {
                return NotFound("Cetățeanul nu a fost găsit.");
            }

            ViewBag.Cetatean = cetatean;
            return View(colectari);
        }
    }
}
