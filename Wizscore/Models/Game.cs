namespace Wizscore.Models
{
    public class Game
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public int NumberOfPlayers { get; set; }
        public int PlayerCreatorId { get; set; }
        public bool HasStarted { get; set; }

        public List<Player> Players { get; set; } = new List<Player>();
    }
}
