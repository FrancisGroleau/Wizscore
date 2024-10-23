namespace Wizscore.Models
{
    public class Score
    {
        public List<RoundScore> RoundsScores { get; set; } = new List<RoundScore>();
        public List<PlayerScore> PlayerScores { get; set; } = new List<PlayerScore>();
    }

    public class RoundScore
    {
        public int RoundNumber { get; set; }
        public string Username { get; set; } = string.Empty;
        public int ActualValue { get; set; }
        public int BidValue { get; set; }
        public int Score { get; set; }
    }

    public class PlayerScore()
    {
        public string Username { get; set; } = string.Empty;
        public int Score { get; set; }
    }

}
