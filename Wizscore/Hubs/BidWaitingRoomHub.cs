using Microsoft.AspNetCore.SignalR;

namespace Wizscore.Hubs
{
    public interface IBidWaitingRoomHub
    {
        Task BidSubmittedAsync();
    }

    public class BidWaitingRoomHub : Hub<IBidWaitingRoomHub>
    {
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
    }
}
