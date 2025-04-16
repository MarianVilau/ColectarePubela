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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Pubela>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<Colectare>()
                .HasOne<Pubela>()
                .WithMany(p => p.Colectari)
                .HasForeignKey(c => c.IdPubela)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PubelaCetatean>()
                .HasOne(pc => pc.Pubela)
                .WithMany(p => p.PubeleCetateni)
                .HasForeignKey(pc => pc.PubelaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PubelaCetatean>()
                .HasOne(pc => pc.Cetatean)
                .WithMany(c => c.PubeleCetateni)
                .HasForeignKey(pc => pc.CetateanId);
        }
    }
}