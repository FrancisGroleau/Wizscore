using Microsoft.EntityFrameworkCore;
using Wizscore.Models;

namespace Wizscore.Persistence.Repositories
{
    public interface IGameRepository
    {
        Task<Game> CreateGameAsync(int numberOfPlayers, string gameKey);
        Task<bool> CheckIfGameKeyExistsAsync(string gameKey);
        Task<Game?> GetGameByKeyAsync(string gameKey);
        Task SetGamePlayerCreatorIdAsync(int gameId, int playerCreatorId);
        Task SetGameHasStartAsync(int gameId);
        Task SetGameNumbersOfPlayerAsync(int gameId, int numberOfPlayers);
        Task<Game> RemovePlayerAsync(int gameId, int playerId);
    }

    public class GameRepository : IGameRepository
    {
        private readonly WizscoreContext _context;

        public GameRepository(WizscoreContext context)
        {
            _context = context;
        }

        public async Task<bool> CheckIfGameKeyExistsAsync(string gameKey)
        {
            return await _context.Games.AnyAsync(a => a.Key == gameKey);
        }

        public async Task<Game> CreateGameAsync(int numberOfPlayers, string gameKey)
        {
            var entity = new Entities.Game()
            {
                NumberOfPlayers = numberOfPlayers,
                Key = gameKey
            };
            _context.Games.Add(entity);
            await _context.SaveChangesAsync();

            return ToModel(entity);
        }

        public async Task<Game?> GetGameByKeyAsync(string gameKey)
        {
            var entity = await _context.Games
                .Include(g => g.Players)
                .Include(g => g.Rounds)
                .ThenInclude(r => r.Bids)
                .FirstOrDefaultAsync(f => f.Key == gameKey);

            if (entity == null)
            {
                return null;
            }

            return ToModel(entity);
      
        }

        public async Task<Game> RemovePlayerAsync(int gameId, int id)
        {
            await _context.Players.Where(w => w.GameId == gameId && w.Id == id).ExecuteDeleteAsync();
            var entity = await _context.Games.FirstAsync(f => f.Id == gameId);

            return ToModel(entity);
        }

        public async Task SetGameHasStartAsync(int gameId)
        {
            var entity = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
            if(entity == null)
            {
                return;
            }

            entity.HasStarted = true;
            _context.Update(entity);

            await _context.SaveChangesAsync();
        }

        public async Task SetGameNumbersOfPlayerAsync(int gameId, int numberOfPlayers)
        {
            var entity = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
            if (entity == null)
            {
                return;
            }

            entity.NumberOfPlayers = numberOfPlayers;
            _context.Update(entity);

            await _context.SaveChangesAsync();
        }

        public async Task SetGamePlayerCreatorIdAsync(int gameId, int playerCreatorId)
        {
            var entity = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
            if (entity == null)
            {
                return;
            }

            entity.PlayerCreatorId = playerCreatorId;
            _context.Update(entity);

            await _context.SaveChangesAsync();
        }

        private static Game ToModel(Entities.Game entity)
        {
            return new Game()
            {
                Id = entity.Id,
                Key = entity.Key,
                NumberOfPlayers = entity.NumberOfPlayers,
                PlayerCreatorId = entity.PlayerCreatorId,
                HasStarted = entity.HasStarted,
                Players = entity.Players
                                  ?.OrderBy(o => o.PlayerNumber)
                                  ?.Select(s => new Player()
                                  {
                                      Id = s.Id,
                                      Username = s.Username,
                                      PlayerNumber = s.PlayerNumber
                                  })?.ToList() ?? new List<Player>(),
                Rounds = entity.Rounds
                ?.OrderBy(o => o.RoundNumber)
                ?.Select(s => new Round()
                {
                    Id = s.Id,
                    DealerId = s.DealerId,
                    GameId = s.GameId,
                    RoundNumber = s.RoundNumber,
                    Suit = s.Suit,
                    Bids = s.Bids?.Select(b => new Bid()
                    {
                        Id = b.Id,
                        BidValue = b.BidValue,
                        ActualValue = b.ActualValue,
                        PlayerId = b.PlayerId,
                        RoundId = b.RoundId
                    })?.ToList() ?? new List<Bid>(),
                })?.ToList() ?? new List<Round>()
            };
        }
    }
}
