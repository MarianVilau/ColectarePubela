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
        public IActionResult Create()
        {
            var model = new Colectare
            {
                IdPubela = "0", // Initialize the required property with a string value
                CollectedAt = DateTime.Now // Initialize the required property
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Colectare colectare)
        {
            if (ModelState.IsValid)
            {
                _context.Colectari.Add(colectare);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(colectare);
        }
    }
}