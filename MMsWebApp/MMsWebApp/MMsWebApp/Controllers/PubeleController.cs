using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMsWebApp.Data;
using MMsWebApp.Models;

namespace MMsWebApp.Controllers
{
    [ApiController]
    [Route("api/pubele")]
    public class PubeleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PubeleController(AppDbContext context)
        {
            _context = context;
        }

        // Adaugă o pubelă
        [HttpPost]
        public IActionResult PostPubela([FromBody] Pubela pubela)
        {
            if (pubela == null)
            {
                return BadRequest();
            }

            _context.Pubele.Add(pubela);
            _context.SaveChanges();

            return Ok(pubela);
        }

        // Obține toate pubelele
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pubela>>> GetPubele()
        {
            return await _context.Pubele.ToListAsync();
        }

        // Obține o pubelă după ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Pubela>> GetPubela(int id)
        {
            var pubela = await _context.Pubele.FindAsync(id);

            if (pubela == null)
            {
                return NotFound();
            }

            return pubela;
        }

        // Modifică o pubelă existentă
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPubela(int id, [FromBody] Pubela pubela)
        {
            if (id != pubela.Id)
            {
                return BadRequest();
            }

            _context.Entry(pubela).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Pubele.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // Șterge o pubelă
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePubela(int id)
        {
            var pubela = await _context.Pubele.FindAsync(id);
            if (pubela == null)
            {
                return NotFound();
            }

            _context.Pubele.Remove(pubela);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
