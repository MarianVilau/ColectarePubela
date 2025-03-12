using Microsoft.EntityFrameworkCore;

namespace MMsWebApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }

}
