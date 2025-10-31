using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wizscore.Models;
using Wizscore.Persistence;
using Wizscore.Persistence.Entities;
using Wizscore.ViewModels;

namespace Wizscore.Persistence.Repositories
{
    public interface ISnakeScoreRepository
    {
        Task AddScoreAsync(SnakeScore score);
        Task<List<SnakeScoreModel>> GetTopScoresAsync(int count = 5);
    }

    public class SnakeScoreRepository : ISnakeScoreRepository
    {
        private readonly WizscoreContext _context;
        public SnakeScoreRepository(WizscoreContext context)
        {
            _context = context;
        }

        public async Task AddScoreAsync(SnakeScore score)
        {
            await _context.SnakeScores.AddAsync(score);
            await _context.SaveChangesAsync();
        }

        public async Task<List<SnakeScoreModel>> GetTopScoresAsync(int count = 5)
        {
            return await _context.SnakeScores
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.Date)
                .Take(count)
                .Select(s => new SnakeScoreModel {
                    Score = s.Score,
                    PlayerName = s.PlayerName
                })
                .ToListAsync();
        }
    }
}
