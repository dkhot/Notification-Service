using FluentAssertions;
using SharedKernel;
using CallbackEntity = Notification.Domain.Entities.Callback;

namespace Notification.Tests.Domain
{
    public sealed class CallbackTests
    {
        [Fact]
        public void ScheduleRetry_IncrementsRetryCountAndSchedulesBackoff_WhenAttemptsRemain()
        {
            var callback = CallbackEntity.Create(Guid.NewGuid(), "inventory", "https://example.com/webhook");

            var exhausted = callback.ScheduleRetry("timeout");

            exhausted.Should().BeFalse();
            callback.RetryCount.Should().Be(1);
            callback.Status.Should().Be(CallbackStatus.Pending);
            callback.NextAttemptAt.Should().BeAfter(DateTimeOffset.UtcNow);
        }

        [Fact]
        public void ScheduleRetry_MarksFailed_WhenMaxAttemptsReached()
        {
            var callback = CallbackEntity.Create(Guid.NewGuid(), "inventory", "https://example.com/webhook");

            bool exhausted = false;
            for (var i = 0; i < CallbackEntity.MaxAttempts; i++)
            {
                exhausted = callback.ScheduleRetry("timeout");
            }

            exhausted.Should().BeTrue();
            callback.RetryCount.Should().Be(CallbackEntity.MaxAttempts);
            callback.Status.Should().Be(CallbackStatus.Failed);
        }

        [Fact]
        public void MarkCompleted_SetsStatusCompletedAndClearsFailureReason()
        {
            var callback = CallbackEntity.Create(Guid.NewGuid(), "inventory", "https://example.com/webhook");
            callback.ScheduleRetry("timeout");

            callback.MarkCompleted();

            callback.Status.Should().Be(CallbackStatus.Completed);
            callback.FailureReason.Should().BeNull();
        }
    }
}
