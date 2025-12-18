using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Voltyks.API.Hubs
{
    /// <summary>
    /// Hub for real-time payment/process status updates
    /// </summary>
    [Authorize]
    public class ProcessHub : Hub
    {
        /// <summary>
        /// Join a group for a specific process
        /// </summary>
        public async Task JoinProcessGroup(int processId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"process-{processId}");
        }

        /// <summary>
        /// Leave a process group
        /// </summary>
        public async Task LeaveProcessGroup(int processId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"process-{processId}");
        }

        /// <summary>
        /// Join user-specific group for receiving payment updates
        /// </summary>
        public async Task JoinUserPaymentGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"payment-user-{userId}");
        }

        /// <summary>
        /// Leave user payment group
        /// </summary>
        public async Task LeaveUserPaymentGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"payment-user-{userId}");
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("uid")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"payment-user-{userId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("uid")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"payment-user-{userId}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
