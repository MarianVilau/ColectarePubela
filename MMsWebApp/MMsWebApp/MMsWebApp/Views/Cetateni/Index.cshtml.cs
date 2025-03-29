using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMsWebApp.Data;
using MMsWebApp.Models;

namespace MMsWebApp.Views.Cetateni
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
            Cetatean = new Cetatean
            {
                Nume = string.Empty,
                Prenume = string.Empty,
                Email = string.Empty,
                CNP = string.Empty
            };
        }

        public Cetatean Cetatean { get; set; }

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

            _context.Cetateni.Add(Cetatean);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
