using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMsWebApp.Data;
using MMsWebApp.Models;

namespace MMsWebApp.Views.Colectari
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
            Colectare = new Colectare
            {
                IdPubela = string.Empty,
                CollectedAt = DateTime.Now
            };
        }

        [BindProperty]
        public Colectare Colectare { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Colectari.Add(Colectare);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}