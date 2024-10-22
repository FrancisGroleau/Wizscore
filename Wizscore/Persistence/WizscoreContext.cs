using Microsoft.EntityFrameworkCore;
using Wizscore.Persistence.Entities;
using Wizscore.Persistence.Entity;

namespace Wizscore.Persistence
{
    public class WizscoreContext : DbContext
    {
        public DbSet<Game> Games { get; set; } = null!;
        public DbSet<Player> Players { get; set; } = null!;

        public string DbPath { get; }

     
        public WizscoreContext(DbContextOptions<WizscoreContext> options) : base(options) { }

    }
}
