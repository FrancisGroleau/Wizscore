using Microsoft.EntityFrameworkCore;
using Wizscore.Models;

namespace Wizscore.Persistence.Repositories
{
    public interface IBidRepository
    {
        Task<Bid> CreateBidAsync(int roundId, int playerId, int bidValue);
        Task UpdateBidActualValueAsync(int bidId, int actualValue);
    }

    public class BidRepository : IBidRepository
    {
        private readonly WizscoreContext _context;

        public BidRepository(WizscoreContext context)
        {
            _context = context;
        }

        public async Task<Bid> CreateBidAsync(int roundId, int playerId, int bidValue)
        {
            var entity = new Entities.Bid()
            {
                PlayerId = playerId,
                RoundId = roundId,
                BidValue = bidValue
            };

            await _context.Bids.AddAsync(entity);
            await _context.SaveChangesAsync();

            return ToModel(entity);
        }

        public async Task UpdateBidActualValueAsync(int bidId, int actualValue)
        {
            await _context.Bids
                .Where(w => w.Id == bidId)
                .ExecuteUpdateAsync((setter) => setter.SetProperty(p => p.ActualValue, actualValue));
        }

        private static Bid ToModel(Entities.Bid entity)
        {
            return new Bid()
            {
                Id = entity.Id,
                RoundId = entity.RoundId,
                PlayerId = entity.PlayerId,
                BidValue = entity.BidValue,
                ActualValue = entity.ActualValue
            };
        }
    }
}
