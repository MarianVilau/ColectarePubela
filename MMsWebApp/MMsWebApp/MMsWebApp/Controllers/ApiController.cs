using Microsoft.AspNetCore.Mvc;
using MMsWebApp.Data;
using MMsWebApp.Models;

namespace MMsWebApp.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ApiController> _logger;

        public ApiController(AppDbContext context, ILogger<ApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("data")]
        public async Task<IActionResult> ReceiveData([FromBody] CollectionData data)
        {
            try
            {
                _logger.LogInformation($"Received data for pubela: {data.IdPubela} at {data.CollectedAt}");

                // Verificăm dacă pubela există
                var pubela = await _context.Pubele.FindAsync(data.IdPubela);
                if (pubela == null)
                {
                    _logger.LogWarning($"Pubela necunoscută: {data.IdPubela}");
                    // Opțional: Puteți crea automat pubela dacă nu există
                    pubela = new Pubela
                    {
                        Id = data.IdPubela,
                        Tip = "Nespecificat"  // Tip default
                    };
                    _context.Pubele.Add(pubela);
                    await _context.SaveChangesAsync();
                }

                // Creăm înregistrarea de colectare
                var colectare = new Colectare
                {
                    IdPubela = data.IdPubela,
                    CollectedAt = data.CollectedAt
                };

                _context.Colectari.Add(colectare);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Colectare salvată cu succes pentru pubela {data.IdPubela}");
                return Ok(new { 
                    success = true, 
                    message = "Colectare înregistrată cu succes",
                    data = new {
                        id = colectare.Id,
                        idPubela = colectare.IdPubela,
                        collectedAt = colectare.CollectedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare la procesarea datelor de colectare");
                return StatusCode(500, new { 
                    success = false, 
                    message = "Eroare la salvarea datelor",
                    error = ex.Message
                });
            }
        }

        // Endpoint pentru verificarea stării API-ului
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new { 
                status = "running",
                timestamp = DateTime.UtcNow
            });
        }

        // Endpoint pentru verificarea unei pubele
        [HttpGet("pubela/{id}")]
        public async Task<IActionResult> GetPubela(string id)
        {
            var pubela = await _context.Pubele.FindAsync(id);
            if (pubela == null)
            {
                return NotFound(new { 
                    success = false, 
                    message = $"Pubela cu ID-ul {id} nu există" 
                });
            }

            var colectari = _context.Colectari
                .Where(c => c.IdPubela == id)
                .OrderByDescending(c => c.CollectedAt)
                .Take(5)  // Ultimele 5 colectări
                .ToList();

            return Ok(new { 
                success = true,
                pubela = new {
                    id = pubela.Id,
                    tip = pubela.Tip
                },
                ultimeleColectari = colectari.Select(c => new {
                    id = c.Id,
                    collectedAt = c.CollectedAt
                })
            });
        }
    }
}
