using SharedKernel;

namespace Notification.Domain.Entities
{
    public sealed class Callback
    {
        public const int MaxAttempts = 5;

        public Guid Id { get; private set; }
        public Guid NotificationId { get; private set; }
        public string SourceId { get; private set; } = null!;
        public string WebhookUrl { get; private set; } = null!;
        public int RetryCount { get; private set; }
        public CallbackStatus Status { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? LastAttemptAt { get; private set; }
        public DateTimeOffset NextAttemptAt { get; private set; }
        public string? FailureReason { get; private set; }

        private Callback()
        {
        }

        public static Callback Create(Guid notificationId, string sourceId, string webhookUrl)
        {
            var now = DateTimeOffset.UtcNow;
            return new Callback
            {
                Id = Guid.NewGuid(),
                NotificationId = notificationId,
                SourceId = sourceId,
                WebhookUrl = webhookUrl,
                RetryCount = 0,
                Status = CallbackStatus.Pending,
                CreatedAt = now,
                NextAttemptAt = now
            };
        }

        public void MarkAttempt()
        {
            LastAttemptAt = DateTimeOffset.UtcNow;
        }

        public void MarkCompleted()
        {
            Status = CallbackStatus.Completed;
            FailureReason = null;
        }

        /// <summary>Applies the retry policy; returns true once retries are exhausted (Status becomes Failed).</summary>
        public bool ScheduleRetry(string failureReason)
        {
            RetryCount += 1;
            FailureReason = failureReason;

            if (RetryCount >= MaxAttempts)
            {
                Status = CallbackStatus.Failed;
                return true;
            }

            var backoffSeconds = Math.Pow(2, RetryCount) * 5;
            NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(backoffSeconds);
            return false;
        }
    }
}
