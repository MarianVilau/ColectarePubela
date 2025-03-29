using Microsoft.AspNetCore.Mvc;
using MMsWebApp.Data;
using MMsWebApp.Models;

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
        public IActionResult Create()
        {
            var model = new Pubela
            {
                Id = string.Empty,
                Tip = string.Empty
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Pubela pubela)
        {
            if (ModelState.IsValid)
            {
                _context.Pubele.Add(pubela);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(pubela);
        }
    }
}