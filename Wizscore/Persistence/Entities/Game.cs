using System.ComponentModel.DataAnnotations.Schema;
using Wizscore.Persistence.Entities;

namespace Wizscore.Persistence.Entity
{
    public class Game
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public int NumberOfPlayers { get; set; }

        public int PlayerCreatorId { get; set; }

        public bool HasStarted { get; set; }

        public List<Player> Players { get; set; }

        public List<Round> Rounds { get; set; }
    }
}
