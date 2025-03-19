using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMsWebApp.Data;
using MMsWebApp.Models;

namespace MMsWebApp.Controllers
{
    [ApiController]
    [Route("api/pubele_cetateni")]
    public class PubeleCetateniController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PubeleCetateniController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult PostPubelaCetatean([FromBody] PubelaCetatean pubelaCetatean)
        {
            if (pubelaCetatean == null)
            {
                return BadRequest();
            }

            _context.PubeleCetateni.Add(pubelaCetatean);
            _context.SaveChanges();

            return Ok(pubelaCetatean);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PubelaCetatean>>> GetPubeleCetateni()
        {
            return await _context.PubeleCetateni.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PubelaCetatean>> GetPubelaCetatean(int id)
        {
            var pubelaCetatean = await _context.PubeleCetateni.FindAsync(id);

            if (pubelaCetatean == null)
            {
                return NotFound();
            }

            return pubelaCetatean;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutPubelaCetatean(int id, [FromBody] PubelaCetatean pubelaCetatean)
        {
            if (id != pubelaCetatean.Id)
            {
                return BadRequest();
            }

            _context.Entry(pubelaCetatean).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.PubeleCetateni.Any(e => e.Id == id))
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
        public async Task<IActionResult> DeletePubelaCetatean(int id)
        {
            var pubelaCetatean = await _context.PubeleCetateni.FindAsync(id);
            if (pubelaCetatean == null)
            {
                return NotFound();
            }

            _context.PubeleCetateni.Remove(pubelaCetatean);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}