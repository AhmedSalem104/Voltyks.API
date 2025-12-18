using Microsoft.AspNetCore.SignalR;
using Voltyks.API.Hubs;
using Voltyks.Application.Interfaces.SignalR;

namespace Voltyks.API.Services
{
    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<ChargingRequestHub> _chargingRequestHub;
        private readonly IHubContext<ProcessHub> _processHub;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly ILogger<SignalRService> _logger;

        public SignalRService(
            IHubContext<ChargingRequestHub> chargingRequestHub,
            IHubContext<ProcessHub> processHub,
            IHubContext<NotificationHub> notificationHub,
            ILogger<SignalRService> logger)
        {
            _chargingRequestHub = chargingRequestHub;
            _processHub = processHub;
            _notificationHub = notificationHub;
            _logger = logger;
        }

        // ============================================
        // Charging Request Events
        // ============================================

        public async Task SendNewRequestAsync(int requestId, string recipientUserId, object data, CancellationToken ct = default)
        {
            try
            {
                // Send to charger owner (recipient)
                await _chargingRequestHub.Clients
                    .Group($"user-{recipientUserId}")
                    .SendAsync("NewRequest", new { requestId, data }, ct);

                _logger.LogInformation("SignalR: NewRequest sent to user {UserId} for request {RequestId}", recipientUserId, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send NewRequest for request {RequestId}", requestId);
            }
        }

        public async Task SendRequestAcceptedAsync(int requestId, string requesterId, object data, CancellationToken ct = default)
        {
            try
            {
                // Send to request group and requester
                await Task.WhenAll(
                    _chargingRequestHub.Clients.Group($"request-{requestId}")
                        .SendAsync("RequestAccepted", new { requestId, status = "accepted", data }, ct),
                    _chargingRequestHub.Clients.Group($"user-{requesterId}")
                        .SendAsync("RequestAccepted", new { requestId, status = "accepted", data }, ct)
                );

                _logger.LogInformation("SignalR: RequestAccepted sent for request {RequestId}", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send RequestAccepted for request {RequestId}", requestId);
            }
        }

        public async Task SendRequestRejectedAsync(int requestId, string requesterId, object data, CancellationToken ct = default)
        {
            try
            {
                await Task.WhenAll(
                    _chargingRequestHub.Clients.Group($"request-{requestId}")
                        .SendAsync("RequestRejected", new { requestId, status = "rejected", data }, ct),
                    _chargingRequestHub.Clients.Group($"user-{requesterId}")
                        .SendAsync("RequestRejected", new { requestId, status = "rejected", data }, ct)
                );

                _logger.LogInformation("SignalR: RequestRejected sent for request {RequestId}", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send RequestRejected for request {RequestId}", requestId);
            }
        }

        public async Task SendRequestConfirmedAsync(int requestId, string requesterId, object data, CancellationToken ct = default)
        {
            try
            {
                await Task.WhenAll(
                    _chargingRequestHub.Clients.Group($"request-{requestId}")
                        .SendAsync("RequestConfirmed", new { requestId, status = "confirmed", data }, ct),
                    _chargingRequestHub.Clients.Group($"user-{requesterId}")
                        .SendAsync("RequestConfirmed", new { requestId, status = "confirmed", data }, ct)
                );

                _logger.LogInformation("SignalR: RequestConfirmed sent for request {RequestId}", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send RequestConfirmed for request {RequestId}", requestId);
            }
        }

        public async Task SendRequestAbortedAsync(int requestId, string targetUserId, object data, CancellationToken ct = default)
        {
            try
            {
                await Task.WhenAll(
                    _chargingRequestHub.Clients.Group($"request-{requestId}")
                        .SendAsync("RequestAborted", new { requestId, status = "aborted", data }, ct),
                    _chargingRequestHub.Clients.Group($"user-{targetUserId}")
                        .SendAsync("RequestAborted", new { requestId, status = "aborted", data }, ct)
                );

                _logger.LogInformation("SignalR: RequestAborted sent for request {RequestId}", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send RequestAborted for request {RequestId}", requestId);
            }
        }

        public async Task SendRequestStatusChangedAsync(int requestId, string targetUserId, string status, object data, CancellationToken ct = default)
        {
            try
            {
                await Task.WhenAll(
                    _chargingRequestHub.Clients.Group($"request-{requestId}")
                        .SendAsync("RequestStatusChanged", new { requestId, status, data }, ct),
                    _chargingRequestHub.Clients.Group($"user-{targetUserId}")
                        .SendAsync("RequestStatusChanged", new { requestId, status, data }, ct)
                );

                _logger.LogInformation("SignalR: RequestStatusChanged to {Status} for request {RequestId}", status, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send RequestStatusChanged for request {RequestId}", requestId);
            }
        }

        // ============================================
        // Process/Payment Events
        // ============================================

        public async Task SendProcessCreatedAsync(int processId, string chargerOwnerId, object data, CancellationToken ct = default)
        {
            try
            {
                await _processHub.Clients
                    .Group($"payment-user-{chargerOwnerId}")
                    .SendAsync("ProcessCreated", new { processId, data }, ct);

                _logger.LogInformation("SignalR: ProcessCreated sent to user {UserId} for process {ProcessId}", chargerOwnerId, processId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send ProcessCreated for process {ProcessId}", processId);
            }
        }

        public async Task SendPaymentStatusChangedAsync(int processId, string targetUserId, string status, object data, CancellationToken ct = default)
        {
            try
            {
                await Task.WhenAll(
                    _processHub.Clients.Group($"process-{processId}")
                        .SendAsync("PaymentStatusChanged", new { processId, status, data }, ct),
                    _processHub.Clients.Group($"payment-user-{targetUserId}")
                        .SendAsync("PaymentStatusChanged", new { processId, status, data }, ct)
                );

                _logger.LogInformation("SignalR: PaymentStatusChanged to {Status} for process {ProcessId}", status, processId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send PaymentStatusChanged for process {ProcessId}", processId);
            }
        }

        public async Task SendPaymentCompletedAsync(int processId, string targetUserId, object data, CancellationToken ct = default)
        {
            try
            {
                await Task.WhenAll(
                    _processHub.Clients.Group($"process-{processId}")
                        .SendAsync("PaymentCompleted", new { processId, status = "completed", data }, ct),
                    _processHub.Clients.Group($"payment-user-{targetUserId}")
                        .SendAsync("PaymentCompleted", new { processId, status = "completed", data }, ct)
                );

                _logger.LogInformation("SignalR: PaymentCompleted sent for process {ProcessId}", processId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send PaymentCompleted for process {ProcessId}", processId);
            }
        }

        public async Task SendPaymentAbortedAsync(int processId, string targetUserId, object data, CancellationToken ct = default)
        {
            try
            {
                await Task.WhenAll(
                    _processHub.Clients.Group($"process-{processId}")
                        .SendAsync("PaymentAborted", new { processId, status = "aborted", data }, ct),
                    _processHub.Clients.Group($"payment-user-{targetUserId}")
                        .SendAsync("PaymentAborted", new { processId, status = "aborted", data }, ct)
                );

                _logger.LogInformation("SignalR: PaymentAborted sent for process {ProcessId}", processId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send PaymentAborted for process {ProcessId}", processId);
            }
        }

        public async Task SendProcessStartedAsync(int processId, string targetUserId, object data, CancellationToken ct = default)
        {
            try
            {
                await Task.WhenAll(
                    _processHub.Clients.Group($"process-{processId}")
                        .SendAsync("ProcessStarted", new { processId, status = "started", data }, ct),
                    _processHub.Clients.Group($"payment-user-{targetUserId}")
                        .SendAsync("ProcessStarted", new { processId, status = "started", data }, ct)
                );

                _logger.LogInformation("SignalR: ProcessStarted sent for process {ProcessId}", processId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send ProcessStarted for process {ProcessId}", processId);
            }
        }

        // ============================================
        // Notification Events
        // ============================================

        public async Task SendNotificationAsync(string userId, string title, string body, object? data = null, CancellationToken ct = default)
        {
            try
            {
                await _notificationHub.Clients
                    .Group($"notifications-{userId}")
                    .SendAsync("ReceiveNotification", new { title, body, data, timestamp = DateTime.UtcNow }, ct);

                _logger.LogInformation("SignalR: Notification sent to user {UserId}: {Title}", userId, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send notification to user {UserId}", userId);
            }
        }

        public async Task SendBroadcastAsync(string title, string body, object? data = null, CancellationToken ct = default)
        {
            try
            {
                await _notificationHub.Clients
                    .Group("broadcast")
                    .SendAsync("ReceiveBroadcast", new { title, body, data, timestamp = DateTime.UtcNow }, ct);

                _logger.LogInformation("SignalR: Broadcast sent: {Title}", title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to send broadcast");
            }
        }
    }
}
