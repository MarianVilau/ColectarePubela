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
        public IActionResult Create()
        {
            var model = new Cetatean
            {
                Nume = string.Empty, 
                Prenume = string.Empty, 
                Email = string.Empty,
                CNP = string.Empty 
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Cetatean cetatean)
        {
            if (ModelState.IsValid)
            {
                _context.Cetateni.Add(cetatean);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(cetatean);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cetateni = await _context.Cetateni.ToListAsync();
            return View(cetateni);
        }
    }
}
