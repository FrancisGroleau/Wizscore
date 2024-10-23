using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Wizscore.Persistence.Entities;
using Wizscore.Models;

namespace Wizscore.Persistence.Configurations
{
    public class RoundConfiguration : IEntityTypeConfiguration<Entities.Round>
    {
        public void Configure(EntityTypeBuilder<Entities.Round> modelBuilder)
        {
            modelBuilder.Property(p => p.Suit)
                .HasConversion(
                    v => v.ToString(),
                    v => (SuitEnum)Enum.Parse(typeof(SuitEnum), v));
        }
    }
}
