namespace Wizscore.Persistence.Entities
{
    public class Bid
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int RoundId { get; set; }
        public int BidValue { get; set; }
        public int? ActualValue { get; set; }
    }
}
