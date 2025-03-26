using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMsWebApp.Data;
using MMsWebApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MMsWebApp.Views.Colectari
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Colectare> Colectari { get; set; }

        [BindProperty]
        public Colectare NewColectare { get; set; }

        public async Task OnGetAsync()
        {
            Colectari = await _context.Colectari.ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Colectari.Add(NewColectare);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}