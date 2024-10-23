using Microsoft.AspNetCore.SignalR;

namespace Wizscore.Hubs
{
    public interface IScoreHub
    {
        Task BidResultSubmittedAsync();

        Task NextRoundStartedAsync();
    }

    public class ScoreHub : Hub<IScoreHub>
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
