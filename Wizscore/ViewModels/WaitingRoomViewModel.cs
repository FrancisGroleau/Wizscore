using Wizscore.Models;

namespace Wizscore.ViewModels
{
    public class WaitingRoomViewModel
    {
        public string GameKey { get; set; } = string.Empty;
        public int NumberOfPlayer { get; set; }
        public bool IsGameCreator { get; set; }
        public List<WaitingRoomPlayerViewModel> Players { get; set; } = new List<WaitingRoomPlayerViewModel>();
    }

    public class WaitingRoomPlayerViewModel()
    {
        public string Username { get; set; }
    }
}
