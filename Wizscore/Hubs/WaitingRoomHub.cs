using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;

namespace Wizscore.Hubs
{
    public interface IWaitingRoomHub
    {
        Task PlayerAddedAsync(string username);

        Task GameStartedAsync();
    }

    public class WaitingRoomHub : Hub<IWaitingRoomHub>
    {

        public WaitingRoomHub()
        {
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
        
            if(httpContext != null)
            {
                var gameKey = httpContext.Request.Cookies[Constants.Cookies.GameKey];
                if(!string.IsNullOrEmpty(gameKey))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, gameKey);
                }
            }
            
          
            await base.OnConnectedAsync();
        }
    }
}
