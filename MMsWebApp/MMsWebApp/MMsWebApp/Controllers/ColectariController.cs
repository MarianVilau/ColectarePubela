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
    public IActionResult PostColectari([FromBody] Colectari colectari)
    {
        if (colectari == null)
        {
            return BadRequest();
        }

        _context.Colectari.Add(colectari);
        _context.SaveChanges();

        return Ok(colectari);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Colectari>>> GetColectari()
    {
        return await _context.Colectari.ToListAsync();
    }
}
}