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
        public DbSet<Cetatean> Cetateni { get; set; }
        public DbSet<PubelaCetatean> PubeleCetateni { get; set; }
        public DbSet<Pubela> Pubele { get; set; }

    }
}