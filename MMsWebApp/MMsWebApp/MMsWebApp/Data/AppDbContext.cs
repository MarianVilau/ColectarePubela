using Microsoft.EntityFrameworkCore;
using MMsWebApp.Models;

namespace MMsWebApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Colectare> Colectari { get; set; }
    }
}