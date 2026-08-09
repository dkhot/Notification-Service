namespace SharedKernel
{
    public enum NotificationChannel
    {
        Email,
        Sms
    }

    public enum NotificationStatus
    {
        Pending,
        Queued,
        Processing,
        Sent,
        Failed,
        CallbackPending,
        CallbackCompleted,
        CallbackFailed
    }

    public enum CallbackStatus
    {
        Pending,
        Completed,
        Failed
    }
}
