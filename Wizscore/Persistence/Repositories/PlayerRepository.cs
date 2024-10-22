using Microsoft.EntityFrameworkCore;
using Wizscore.Models;

namespace Wizscore.Persistence.Repositories
{
    public interface IPlayerRepository
    {
        Task<Player> CreatePlayerAsync(int gameId, string userName);

        Task<Player?> GetPlayerByGameIdAndUsernameAsync(int gameId, string userName);
    }
    public class PlayerRepository : IPlayerRepository
    {
        private readonly WizscoreContext _context;
        public PlayerRepository(WizscoreContext context)
        {
            _context = context;
        }

        public async Task<Player> CreatePlayerAsync(int gameId, string userName)
        {
            var entity = new Entities.Player()
            {
                Username = userName,
                GameId = gameId,
            };

            _context.Players.Add(entity);
            await _context.SaveChangesAsync();

            var model = new Player()
            {
                Id = entity.Id,
                Username = entity.Username
            };

            return model;
        }

        public async Task<Player?> GetPlayerByGameIdAndUsernameAsync(int gameId, string userName)
        {
            var entity = await _context.Players.FirstOrDefaultAsync(f => f.GameId == gameId && f.Username == userName);
            if(entity == null)
            {
                return null;
            }

            var model = new Player()
            {
                Id = entity.Id,
                Username = entity.Username,
            };

            return model;
        }
    }
}
