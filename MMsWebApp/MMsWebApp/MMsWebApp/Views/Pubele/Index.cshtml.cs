using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMsWebApp.Data;
using MMsWebApp.Models;
using System.Threading.Tasks;

namespace MMsWebApp.Views.Pubele
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
            Pubela = new Pubela
            {
                Id = string.Empty,
                Tip = string.Empty
            };
        }

        [BindProperty]
        public Pubela Pubela { get; set; }

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

            _context.Pubele.Add(Pubela);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}