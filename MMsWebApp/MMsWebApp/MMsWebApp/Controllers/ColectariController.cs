using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMsWebApp.Data;
using MMsWebApp.Models;

namespace MMsWebApp.Controllers
{
    [Route("/api/data")]
    [ApiController]
    public class ColectariController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ColectariController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostNodeData([FromBody] Colectari colectari)
        {
            if (colectari == null)
            {
                return BadRequest();
            }

            _context.Colectari.Add(colectari);
            await _context.SaveChangesAsync();

            return Ok(colectari);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Colectari>>> GetColectari()
        {
            return await _context.Colectari.ToListAsync();
        }
    }

}
