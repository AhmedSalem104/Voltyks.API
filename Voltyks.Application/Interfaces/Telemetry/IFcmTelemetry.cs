using System.Collections.Generic;

namespace Voltyks.Application.Interfaces.Telemetry
{
    public interface IFcmTelemetry
    {
        void RecordSent(string notificationType);
        void RecordFailed(string notificationType, string errorCode);
        FcmTelemetrySnapshot GetSnapshot();
    }

    public record FcmTelemetrySnapshot(
        long Sent,
        long Failed,
        IReadOnlyDictionary<string, long> FailedByErrorCode,
        IReadOnlyDictionary<string, long> FailedByNotificationType);
}
