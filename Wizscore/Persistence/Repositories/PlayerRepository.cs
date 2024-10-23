using Microsoft.EntityFrameworkCore;
using Wizscore.Models;

namespace Wizscore.Persistence.Repositories
{
    public interface IPlayerRepository
    {
        Task<Player> CreatePlayerAsync(int gameId, string userName, int playerNumber);
        Task<Player?> GetPlayerByGameIdAndUsernameAsync(int gameId, string userName);
        Task<Player?> GetPlayerByIdAsync(int playerId);
        Task UpdatePlayerNumberAsync(int playerId, int playerNumber);
    }
    public class PlayerRepository : IPlayerRepository
    {
        private readonly WizscoreContext _context;
        public PlayerRepository(WizscoreContext context)
        {
            _context = context;
        }

        public async Task<Player> CreatePlayerAsync(int gameId, string userName, int playerNumber)
        {
            var entity = new Entities.Player()
            {
                Username = userName,
                GameId = gameId,
                PlayerNumber = playerNumber
            };

            _context.Players.Add(entity);
            await _context.SaveChangesAsync();

            return ToModel(entity);
        }

        public async Task<Player?> GetPlayerByGameIdAndUsernameAsync(int gameId, string userName)
        {
            var entity = await _context.Players.FirstOrDefaultAsync(f => f.GameId == gameId && f.Username == userName);
            if(entity == null)
            {
                return null;
            }

            
            return ToModel(entity);
        }

        public async Task<Player?> GetPlayerByIdAsync(int playerId)
        {
            var entity = await _context.Players.FirstOrDefaultAsync(f => f.Id == playerId);
            if (entity == null)
            {
                return null;
            }

            return ToModel(entity);
        }

        public async Task UpdatePlayerNumberAsync(int playerId, int playerNumber)
        {
            await _context.Players
                .Where(w => w.Id == playerId)
                .ExecuteUpdateAsync((setter) => setter.SetProperty(p => p.PlayerNumber, playerNumber));
        }

        private static Player ToModel(Entities.Player entity)
        {
            return new Player()
            {
                Id = entity.Id,
                Username = entity.Username,
                PlayerNumber = entity.PlayerNumber
            };
        }
    }
}
