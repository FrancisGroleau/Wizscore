using Microsoft.AspNetCore.SignalR;
using Wizscore.Models;

namespace Wizscore.Hubs
{
    public interface IBidHub
    {
        Task SuitChangedAsync(string suit);

        Task BidSubmittedAsync(string username, int bid);
    }

    public class BidHub : Hub<IBidHub>
    {
        public async Task SuitChanged(string suit)
        {
            var suitValue = ToSuitEnum(suit);
            var httpContext = Context.GetHttpContext();

            if (httpContext != null)
            {
                var gameKey = httpContext.Request.Cookies[Constants.Cookies.GameKey];
                if (!string.IsNullOrEmpty(gameKey) && suitValue != null)
                {
                    await Clients.Group(gameKey).SuitChangedAsync(suitValue.Value.ToString());
                }
            }
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();

            if (httpContext != null)
            {
                var gameKey = httpContext.Request.Cookies[Constants.Cookies.GameKey];
                if (!string.IsNullOrEmpty(gameKey))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, gameKey);
                }
            }


            await base.OnConnectedAsync();
        }

        private static SuitEnum? ToSuitEnum(string suit)
        {
           if(suit.ToLower() == SuitEnum.Club.ToString().ToLower())
            {
                return SuitEnum.Club;
            }

            if (suit.ToLower() == SuitEnum.Heart.ToString().ToLower())
            {
                return SuitEnum.Heart;
            }

            if (suit.ToLower() == SuitEnum.Spade.ToString().ToLower())
            {
                return SuitEnum.Spade;
            }

            if (suit.ToLower() == SuitEnum.Diamond.ToString().ToLower())
            {
                return SuitEnum.Diamond;
            }

            return null;
        }
    }
}
