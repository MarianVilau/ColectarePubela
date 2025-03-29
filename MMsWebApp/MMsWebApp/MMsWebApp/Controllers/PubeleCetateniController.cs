using Microsoft.AspNetCore.Mvc;
using MMsWebApp.Data;
using MMsWebApp.Models;

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
        public IActionResult Create()
        {
            var model = new PubelaCetatean
            {
                PubelaId = string.Empty,
                CetateanId = 0,
                Adresa = string.Empty
            };

            ViewBag.Pubele = _context.Pubele.ToList();
            ViewBag.Cetateni = _context.Cetateni.ToList();

            return View(model);
        }

        [HttpPost]
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
                    ModelState.AddModelError("", "A apărut o eroare la salvare.");
                }
            }

            ViewBag.Pubele = _context.Pubele.ToList();
            ViewBag.Cetateni = _context.Cetateni.ToList();
            return View(pubelaCetatean);
        }
    }
}