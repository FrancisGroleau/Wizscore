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
            var entity = new Entity.Game()
            {
                NumberOfPlayers = numberOfPlayers,
                Key = gameKey
            };
            _context.Games.Add(entity);
            await _context.SaveChangesAsync();

            return new Game()
            {
                Id = entity.Id,
                Key = entity.Key,
                NumberOfPlayers = entity.NumberOfPlayers,
            };
        }

        public async Task<Game?> GetGameByKeyAsync(string gameKey)
        {
            var entity = await _context.Games
                .Include(g => g.Players)
                .FirstOrDefaultAsync(f => f.Key == gameKey);

            if (entity == null)
            {
                return null;
            }

            return new Game()
            {
                Id = entity.Id,
                Key = entity.Key,
                NumberOfPlayers = entity.NumberOfPlayers,
                PlayerCreatorId = entity.PlayerCreatorId,
                HasStarted = entity.HasStarted,
                Players = entity.Players.Select(s => new Player()
                {
                    Id = s.Id,
                    Username = s.Username
                }).ToList()
            };
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
    }
}
