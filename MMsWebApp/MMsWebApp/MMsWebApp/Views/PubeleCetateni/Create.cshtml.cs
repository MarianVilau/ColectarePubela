using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMsWebApp.Data;
using MMsWebApp.Models;
using System.Threading.Tasks;

namespace MMsWebApp.Views.PubeleCetateni
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PubelaCetatean PubelaCetatean { get; set; }

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

            _context.PubeleCetateni.Add(PubelaCetatean);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}