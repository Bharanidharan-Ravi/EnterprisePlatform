using APIPlatform.Notification.Models;
using APIPlatform.Notification.Services;
using APIPlatform.Notification.Tests.Fakes;
using Xunit;

namespace APIPlatform.Notification.Tests.Services;

public class NotificationServiceTests
{
    private static CreateNotificationRequest ValidRequest(params NotificationTargetRule[] targets) => new()
    {
        Application = "PROJECT",
        EventType = "PROJECT_CREATED",
        Title = "Project created",
        Targets = targets.Length > 0 ? targets : [NotificationTargetRule.TargetGroup("PROJECT_TEAM")]
    };

    [Fact]
    public async Task CreateAsync_ValidRequest_GeneratesIdAndTimestamp_AndSucceeds()
    {
        var repository = new FakeNotificationRepository();
        var clock = new FakeClock { UtcNow = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero) };
        var service = new NotificationService(repository, clock);

        var result = await service.CreateAsync(ValidRequest());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.Id));
        Assert.Equal(clock.UtcNow, result.Value.CreatedOnUtc);
        Assert.NotNull(repository.InsertedNotification);
        Assert.Single(repository.InsertedTargets!);
    }

    [Fact]
    public async Task CreateAsync_MissingApplication_FailsWithoutCallingRepository()
    {
        var repository = new FakeNotificationRepository();
        var service = new NotificationService(repository, new FakeClock());

        var result = await service.CreateAsync(ValidRequest() with { Application = "" });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "APPLICATION_REQUIRED");
        Assert.Null(repository.InsertedNotification);
    }

    [Fact]
    public async Task CreateAsync_NoTargets_Fails()
    {
        var service = new NotificationService(new FakeNotificationRepository(), new FakeClock());

        var result = await service.CreateAsync(ValidRequest() with { Targets = [] });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "TARGETS_REQUIRED");
    }

    [Fact]
    public async Task CreateAsync_OnlyExclusionTargets_Fails()
    {
        var service = new NotificationService(new FakeNotificationRepository(), new FakeClock());

        var result = await service.CreateAsync(ValidRequest(NotificationTargetRule.ExcludeUser("U1")));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "NO_INCLUDING_TARGET");
    }

    [Fact]
    public async Task CreateAsync_AllTargetWithValue_Fails()
    {
        var service = new NotificationService(new FakeNotificationRepository(), new FakeClock());
        var badTarget = NotificationTargetRule.TargetAll() with { Value = "should-not-be-set" };

        var result = await service.CreateAsync(ValidRequest(badTarget));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "ALL_TARGET_HAS_VALUE");
    }

    [Fact]
    public async Task CreateAsync_UserTargetWithoutValue_Fails()
    {
        var service = new NotificationService(new FakeNotificationRepository(), new FakeClock());
        var badTarget = NotificationTargetRule.TargetUser("x") with { Value = "  " };

        var result = await service.CreateAsync(ValidRequest(badTarget));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "TARGET_VALUE_REQUIRED");
    }

    [Fact]
    public async Task CreateAsync_EntityTypeWithoutEntityId_Fails()
    {
        var service = new NotificationService(new FakeNotificationRepository(), new FakeClock());

        var result = await service.CreateAsync(ValidRequest() with { EntityType = "PROJECT", EntityId = null });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "ENTITY_CONTEXT_INCOMPLETE");
    }

    [Fact]
    public async Task GetUnreadCountAsync_NoPriorState_CountsSinceNull()
    {
        var repository = new FakeNotificationRepository { UserState = null, CountResult = 5 };
        var service = new NotificationService(repository, new FakeClock());
        var recipient = NotificationRecipient.For("U1");

        var count = await service.GetUnreadCountAsync("PROJECT", recipient);

        Assert.Equal(5, count);
        Assert.Null(repository.CountCalls[0].Since);
    }

    [Fact]
    public async Task GetUnreadCountAsync_WithPriorState_UsesLastReadOnAsSince()
    {
        var lastRead = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var repository = new FakeNotificationRepository
        {
            UserState = new NotificationUserState { UserId = "U1", Application = "PROJECT", LastReadOnUtc = lastRead, UpdatedOnUtc = lastRead }
        };
        var service = new NotificationService(repository, new FakeClock());

        await service.GetUnreadCountAsync("PROJECT", NotificationRecipient.For("U1"));

        Assert.Equal(lastRead, repository.CountCalls[0].Since);
    }

    [Fact]
    public async Task MarkAsReadAsync_DefaultsToClockNow_AndDoesNotTouchSyncCursor()
    {
        var repository = new FakeNotificationRepository();
        var clock = new FakeClock { UtcNow = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero) };
        var service = new NotificationService(repository, clock);

        var result = await service.MarkAsReadAsync("PROJECT", "U1");

        Assert.True(result.Succeeded);
        Assert.Single(repository.LastReadOnCalls);
        Assert.Equal(clock.UtcNow, repository.LastReadOnCalls[0].Value);
        Assert.Empty(repository.LastSyncedOnCalls);
    }

    [Fact]
    public async Task MarkAsSyncedAsync_DoesNotTouchReadCursor()
    {
        var repository = new FakeNotificationRepository();
        var service = new NotificationService(repository, new FakeClock());

        await service.MarkAsSyncedAsync("PROJECT", "U1");

        Assert.Single(repository.LastSyncedOnCalls);
        Assert.Empty(repository.LastReadOnCalls);
    }

    [Fact]
    public async Task MarkAsReadAsync_MissingUserId_FailsWithoutCallingRepository()
    {
        var repository = new FakeNotificationRepository();
        var service = new NotificationService(repository, new FakeClock());

        var result = await service.MarkAsReadAsync("PROJECT", "");

        Assert.False(result.Succeeded);
        Assert.Empty(repository.LastReadOnCalls);
    }

    [Fact]
    public async Task GetNotificationsAsync_PageNumberLessThanOne_Throws()
    {
        var service = new NotificationService(new FakeNotificationRepository(), new FakeClock());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.GetNotificationsAsync("PROJECT", NotificationRecipient.For("U1"), pageNumber: 0, pageSize: 10));
    }

    [Fact]
    public async Task GetNotificationsAsync_PageSizeAboveMax_IsClamped()
    {
        var repository = new FakeNotificationRepository { CountResult = 0, ListResult = [] };
        var service = new NotificationService(repository, new FakeClock());

        var result = await service.GetNotificationsAsync("PROJECT", NotificationRecipient.For("U1"), pageNumber: 1, pageSize: 10_000);

        Assert.Equal(NotificationService.MaxPageSize, result.PageSize);
        Assert.Equal(NotificationService.MaxPageSize, repository.ListCalls[0].Take);
    }

    [Fact]
    public async Task GetNotificationsAsync_ComputesSkipFromPageNumber()
    {
        var repository = new FakeNotificationRepository();
        var service = new NotificationService(repository, new FakeClock());

        await service.GetNotificationsAsync("PROJECT", NotificationRecipient.For("U1"), pageNumber: 3, pageSize: 20);

        Assert.Equal(40, repository.ListCalls[0].Skip);
    }
}
