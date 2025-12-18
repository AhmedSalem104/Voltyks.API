using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Voltyks.API.Hubs
{
    /// <summary>
    /// Hub for general real-time notifications
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        /// <summary>
        /// Join user-specific notification group
        /// </summary>
        public async Task JoinNotificationGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"notifications-{userId}");
        }

        /// <summary>
        /// Leave notification group
        /// </summary>
        public async Task LeaveNotificationGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"notifications-{userId}");
        }

        /// <summary>
        /// Join broadcast group for system-wide announcements
        /// </summary>
        public async Task JoinBroadcastGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "broadcast");
        }

        /// <summary>
        /// Leave broadcast group
        /// </summary>
        public async Task LeaveBroadcastGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "broadcast");
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("uid")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"notifications-{userId}");
            }
            // All authenticated users join broadcast group
            await Groups.AddToGroupAsync(Context.ConnectionId, "broadcast");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("uid")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"notifications-{userId}");
            }
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "broadcast");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
