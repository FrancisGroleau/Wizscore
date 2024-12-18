﻿using Wizscore.Models;

namespace Wizscore.ViewModels
{
    public class WaitingRoomViewModel
    {
        public string GameKey { get; set; } = string.Empty;
        public string ShareUrl { get; set; }
        public int NumberOfPlayer { get; set; }
        public bool IsGameCreator { get; set; }
        public string CurrentUserName { get; set; }
        public List<WaitingRoomPlayerViewModel> Players { get; set; } = new List<WaitingRoomPlayerViewModel>();
    }

    public class WaitingRoomPlayerViewModel()
    {
        public string Username { get; set; }
        public int PlayerNumber { get; set; }
    }
}
