﻿using Wizscore.Models;

namespace Wizscore.Persistence.Entities
{
    public class Round
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int RoundNumber { get; set; }
        public int DealerId { get; set; }
        public SuitEnum Suit { get; set; } 

        public List<Bid> Bids { get; set; }
    }
}
