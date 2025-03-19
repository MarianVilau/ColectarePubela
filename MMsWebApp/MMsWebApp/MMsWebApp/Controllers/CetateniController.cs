using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMsWebApp.Data;
using MMsWebApp.Models;

namespace MMsWebApp.Controllers
{
    [ApiController]
    [Route("api/cetateni")]
    public class CetateniController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CetateniController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult PostCetatean([FromBody] Cetatean cetatean)
        {
            if (cetatean == null)
            {
                return BadRequest();
            }

            if (_context.Cetateni.Any(c => c.CNP == cetatean.CNP))
            {
                return Conflict("A citizen with this CNP already exists.");
            }

            _context.Cetateni.Add(cetatean);
            _context.SaveChanges();

            return Ok(cetatean);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cetatean>>> GetCetateni()
        {
            return await _context.Cetateni.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Cetatean>> GetCetatean(int id)
        {
            var cetatean = await _context.Cetateni.FindAsync(id);

            if (cetatean == null)
            {
                return NotFound();
            }

            return cetatean;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCetatean(int id, [FromBody] Cetatean cetatean)
        {
            if (id != cetatean.Id)
            {
                return BadRequest();
            }

            _context.Entry(cetatean).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Cetateni.Any(e => e.Id == id))
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCetatean(int id)
        {
            var cetatean = await _context.Cetateni.FindAsync(id);
            if (cetatean == null)
            {
                return NotFound();
            }

            _context.Cetateni.Remove(cetatean);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}