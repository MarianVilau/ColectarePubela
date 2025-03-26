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
                Adresa = string.Empty // Initialize the required property
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PubelaCetatean pubelaCetatean)
        {
            if (ModelState.IsValid)
            {
                _context.PubeleCetateni.Add(pubelaCetatean);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(pubelaCetatean);
        }
    }
}