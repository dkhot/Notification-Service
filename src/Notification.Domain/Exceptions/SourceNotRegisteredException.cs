namespace Notification.Domain.Exceptions
{
    public sealed class SourceNotRegisteredException : Exception
    {
        public string SourceId { get; }

        public SourceNotRegisteredException(string sourceId)
            : base($"SourceId '{sourceId}' is not registered or enabled.")
        {
            SourceId = sourceId;
        }
    }
}
