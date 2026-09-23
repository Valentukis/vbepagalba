using Microsoft.EntityFrameworkCore;
using VbeSort.Models;

namespace VbeSort.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
    }
}
