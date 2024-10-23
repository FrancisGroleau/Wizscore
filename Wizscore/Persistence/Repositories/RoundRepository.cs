using Wizscore.Models;

namespace Wizscore.Persistence.Repositories
{
    public interface IRoundRepository
    {
        Task<Round> CreateRoundAsync(int gameId, SuitEnum suit, int dealerId, int roundNumber);
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

            return new Round()
            {
                Id = entity.Id,
                GameId = entity.GameId,
                RoundNumber = entity.RoundNumber,
                Suit = entity.Suit,
                DealerId = entity.DealerId
            };
        }
    }
}
