using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Voltyks.API.Hubs
{
    /// <summary>
    /// Hub for real-time charging request status updates
    /// </summary>
    [Authorize]
    public class ChargingRequestHub : Hub
    {
        /// <summary>
        /// Join a group for a specific charging request
        /// </summary>
        public async Task JoinRequestGroup(int requestId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"request-{requestId}");
        }

        /// <summary>
        /// Leave a charging request group
        /// </summary>
        public async Task LeaveRequestGroup(int requestId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"request-{requestId}");
        }

        /// <summary>
        /// Join user-specific group for receiving personal updates
        /// </summary>
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        /// <summary>
        /// Leave user-specific group
        /// </summary>
        public async Task LeaveUserGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("uid")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("uid")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
