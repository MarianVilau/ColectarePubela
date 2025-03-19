using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMsWebApp.Data;
using MMsWebApp.Models;

namespace MMsWebApp.Controllers
{
    [ApiController]
    [Route("api/data")]
    public class ColectariController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ColectariController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult PostColectari([FromBody] Colectare colectare)
        {
            if (colectare == null)
            {
                return BadRequest();
            }

            try
            {
                _context.Colectari.Add(colectare);
                _context.SaveChanges();
            }
            catch (DbUpdateException ex)
            {
                // Log the exception details
                Console.WriteLine($"An error occurred while saving the entity changes: {ex.InnerException?.Message}");
                return StatusCode(500, "An error occurred while saving the entity changes.");
            }

            return Ok(colectare);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Colectare>>> GetColectari()
        {
            return await _context.Colectari.ToListAsync();
        }
    }
}