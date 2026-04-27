using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Voltyks.Application.Interfaces.Notifications
{
    /// <summary>
    /// Resolves a notification template (key + lang + parameters) into a
    /// (Title, Body) pair. DB-stored templates win when present; otherwise
    /// the resolver falls back to <c>NotificationMessages.cs</c> via the
    /// <see cref="HardcodedTemplateRegistry"/> so an empty
    /// NotificationTemplates table still produces the original wording.
    /// </summary>
    public interface INotificationTemplateResolver
    {
        Task<(string Title, string Body)> ResolveAsync(
            string key,
            string lang,
            IDictionary<string, string>? parameters,
            CancellationToken ct = default);

        /// <summary>Drops the cache for a single template (call after a PUT/DELETE).</summary>
        void Invalidate(string key);
    }
}
