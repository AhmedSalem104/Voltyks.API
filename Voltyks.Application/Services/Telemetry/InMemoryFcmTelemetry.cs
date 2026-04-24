using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Voltyks.Application.Interfaces.Telemetry;

namespace Voltyks.Application.Services.Telemetry
{
    /// <summary>
    /// In-process FCM telemetry backend. Counts stay in memory and reset
    /// on app-pool recycle. Swap the DI registration for an App Insights /
    /// Prometheus / OpenTelemetry implementation when a persistent backend
    /// is available — call sites do not change.
    /// </summary>
    public class InMemoryFcmTelemetry : IFcmTelemetry
    {
        private long _sent;
        private long _failed;
        private readonly ConcurrentDictionary<string, long> _failedByErrorCode = new();
        private readonly ConcurrentDictionary<string, long> _failedByNotificationType = new();
        private readonly ConcurrentDictionary<string, long> _sentByNotificationType = new();

        public void RecordSent(string notificationType)
        {
            Interlocked.Increment(ref _sent);
            _sentByNotificationType.AddOrUpdate(notificationType ?? "UNKNOWN", 1, (_, v) => v + 1);
        }

        public void RecordFailed(string notificationType, string errorCode)
        {
            Interlocked.Increment(ref _failed);
            _failedByErrorCode.AddOrUpdate(errorCode ?? "UNKNOWN", 1, (_, v) => v + 1);
            _failedByNotificationType.AddOrUpdate(notificationType ?? "UNKNOWN", 1, (_, v) => v + 1);
        }

        public FcmTelemetrySnapshot GetSnapshot() => new(
            Sent: Interlocked.Read(ref _sent),
            Failed: Interlocked.Read(ref _failed),
            SentByNotificationType: new Dictionary<string, long>(_sentByNotificationType),
            FailedByErrorCode: new Dictionary<string, long>(_failedByErrorCode),
            FailedByNotificationType: new Dictionary<string, long>(_failedByNotificationType));
    }
}
