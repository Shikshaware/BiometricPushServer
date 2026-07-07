using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace BiometricPushServer.Web.Hubs
{
    /// <summary>
    /// Real-time attendance feed hub.
    /// Clients connect to /hubs/attendance to receive live updates.
    /// </summary>
    public class AttendanceHub : Hub
    {
        public async Task JoinGroup(string clientId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"client_{clientId}");
        }

        public async Task LeaveGroup(string clientId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"client_{clientId}");
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }
    }
}
