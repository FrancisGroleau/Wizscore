namespace Wizscore.ViewModels
{
    public class ScoreViewModel
    {
        public bool IsNextDealer { get; set; }
        public bool IsFinished { get; set; }

        public List<ScoreRoundViewModel> RoundsScores { get; set; } = new List<ScoreRoundViewModel>();

        public List<ScorePlayerViewModel> PlayerScores { get; set; } = new List<ScorePlayerViewModel>();

    }

    public class ScorePlayerViewModel()
    {
        public string Username { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    public class ScoreRoundViewModel()
    {
        public int RoundNumber { get; set; }
        public string Username { get; set; } = string.Empty;
        public int BidValue { get; set; }
        public int? ActualValue { get; set; }
        public int Score { get; set; }
    }

}
