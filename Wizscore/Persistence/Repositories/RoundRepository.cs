using Microsoft.EntityFrameworkCore;
using Wizscore.Models;

namespace Wizscore.Persistence.Repositories
{
    public interface IRoundRepository
    {
        Task<Round> CreateRoundAsync(int gameId, SuitEnum suit, int dealerId, int roundNumber);
        Task UpdateCurrentSuitAsync(int id, SuitEnum suitValue);
    }

    public class RoundRepository : IRoundRepository
    {
        private readonly WizscoreContext _context;

        public RoundRepository(WizscoreContext context)
        {
            _context = context;
        }

        public async Task<Round> CreateRoundAsync(int gameId, SuitEnum suit, int dealerId, int roundNumber)
        {
            var entity = new Entities.Round()
            {
                DealerId = dealerId,
                GameId = gameId,
                RoundNumber = roundNumber,
                Suit = suit
            };

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return ToModel(entity);
        }
        

        private static Round ToModel(Entities.Round entity)
        {
            return new Round()
            {
                Id = entity.Id,
                GameId = entity.GameId,
                RoundNumber = entity.RoundNumber,
                Suit = entity.Suit,
                DealerId = entity.DealerId
            };
        }

        public async Task UpdateCurrentSuitAsync(int id, SuitEnum suitValue)
        {
            await _context.Rounds.Where(w => w.Id == id)
                .ExecuteUpdateAsync((setter) => setter.SetProperty(p => p.Suit, suitValue));
        }
    }
}
