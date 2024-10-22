using Wizscore.Models;

namespace Wizscore.ViewModels
{
    public class BidViewModel
    {
        public bool IsDealer { get; set; }

        public int Bid { get; set; }

        public SuitEnum Suit { get; set; }
    }
}
