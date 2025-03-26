using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMsWebApp.Data;
using MMsWebApp.Models;

namespace MMsWebApp.Controllers
{
    [ApiController]
    [Route("api/data")]
    public class ColectariController : Controller
    {
        private readonly AppDbContext _context;

        public ColectariController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create(Colectare colectare)
        {
            if (colectare == null)
            {
                return BadRequest();
            }

            try
            {
                _context.Colectari.Add(colectare);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the exception details
                Console.WriteLine($"An error occurred while saving the entity changes: {ex.InnerException?.Message}");
                return StatusCode(500, "An error occurred while saving the entity changes.");
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var colectari = await _context.Colectari.ToListAsync();
            var viewModel = new ColectariViewModel
            {
                Colectari = colectari,
                NewColectare = new Colectare
                {
                    IdPubela = string.Empty // Initialize the required property
                }
            };
            return View(viewModel);
        }
    }
}