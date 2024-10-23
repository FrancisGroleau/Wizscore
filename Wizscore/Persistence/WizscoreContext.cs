using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Wizscore.Persistence.Entities;

namespace Wizscore.Persistence
{
    public class WizscoreContext : DbContext
    {
        public DbSet<Game> Games { get; set; } = null!;
        public DbSet<Player> Players { get; set; } = null!;
        public DbSet<Round> Rounds { get; set; } = null!;
        public DbSet<Bid> Bids { get; set; } = null!;


        public WizscoreContext(DbContextOptions<WizscoreContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

    }
}
