namespace Voltyks.Application.Interfaces.SignalR
{
    public interface ISignalRService
    {
        // ============================================
        // Charging Request Events
        // ============================================

        /// <summary>
        /// Send when a new charging request is created (status: pending)
        /// </summary>
        Task SendNewRequestAsync(int requestId, string recipientUserId, object data, CancellationToken ct = default);

        /// <summary>
        /// Send when charging request is accepted
        /// </summary>
        Task SendRequestAcceptedAsync(int requestId, string requesterId, object data, CancellationToken ct = default);

        /// <summary>
        /// Send when charging request is rejected
        /// </summary>
        Task SendRequestRejectedAsync(int requestId, string requesterId, object data, CancellationToken ct = default);

        /// <summary>
        /// Send when charging request is confirmed (session started)
        /// </summary>
        Task SendRequestConfirmedAsync(int requestId, string requesterId, object data, CancellationToken ct = default);

        /// <summary>
        /// Send when charging request is aborted
        /// </summary>
        Task SendRequestAbortedAsync(int requestId, string targetUserId, object data, CancellationToken ct = default);

        /// <summary>
        /// Send generic request status change
        /// </summary>
        Task SendRequestStatusChangedAsync(int requestId, string targetUserId, string status, object data, CancellationToken ct = default);

        // ============================================
        // Process/Payment Events
        // ============================================

        /// <summary>
        /// Send when a new process is created
        /// </summary>
        Task SendProcessCreatedAsync(int processId, string chargerOwnerId, object data, CancellationToken ct = default);

        /// <summary>
        /// Send when payment status changes
        /// </summary>
        Task SendPaymentStatusChangedAsync(int processId, string targetUserId, string status, object data, CancellationToken ct = default);

        /// <summary>
        /// Send when payment/process is completed
        /// </summary>
        Task SendPaymentCompletedAsync(int processId, string targetUserId, object data, CancellationToken ct = default);

        /// <summary>
        /// Send when payment/process is aborted
        /// </summary>
        Task SendPaymentAbortedAsync(int processId, string targetUserId, object data, CancellationToken ct = default);

        /// <summary>
        /// Send when process started (charging session begins)
        /// </summary>
        Task SendProcessStartedAsync(int processId, string targetUserId, object data, CancellationToken ct = default);

        // ============================================
        // Notification Events
        // ============================================

        /// <summary>
        /// Send real-time notification to specific user
        /// </summary>
        Task SendNotificationAsync(string userId, string title, string body, object? data = null, CancellationToken ct = default);

        /// <summary>
        /// Send broadcast notification to all connected users
        /// </summary>
        Task SendBroadcastAsync(string title, string body, object? data = null, CancellationToken ct = default);
    }
}
