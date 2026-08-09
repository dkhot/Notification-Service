namespace Notification.Application.Callbacks
{
    public interface IDispatchPendingCallbackHandler
    {
        /// <summary>Attempts to deliver the next due callback, if any. Returns true if a callback was attempted.</summary>
        Task<bool> DispatchNextAsync(CancellationToken cancellationToken = default);
    }
}
