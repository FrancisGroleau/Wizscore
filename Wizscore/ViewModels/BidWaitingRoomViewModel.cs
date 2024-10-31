using Wizscore.Models;

namespace Wizscore.ViewModels
{
    public class BidWaitingRoomViewModel
    {
        public int RoundNumber { get; set; }

        public SuitEnum Suit { get; set; }

        public List<string> BidMessages { get; set; } = new List<string>();

        public bool IsDealer { get; set; }
        public bool IsRoundFinished { get; set; }
        public string NextBiderUsername { get; set; } = string.Empty;
    }
}
