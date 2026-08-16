using APIPlatform.Foundation.Interfaces;
using APIPlatform.Foundation.Results;
using APIPlatform.Notification.Abstractions;
using APIPlatform.Notification.Models;

namespace APIPlatform.Notification.Services;

/// <summary>Default <see cref="INotificationService"/>: validates input, generates ids/timestamps, and delegates persistence/queries to <see cref="INotificationRepository"/>.</summary>
public sealed class NotificationService : INotificationService
{
    /// <summary>Hard ceiling on page size, independent of what a caller requests, so a misbehaving
    /// client can't force an unbounded read. Documented, not hidden.</summary>
    public const int MaxPageSize = 200;

    private readonly INotificationRepository _repository;
    private readonly IClock _clock;

    public NotificationService(INotificationRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<Result<NotificationRecord>> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0) return Result<NotificationRecord>.Failure(validationErrors.ToArray());

        var notification = new NotificationRecord
        {
            Id = Guid.NewGuid().ToString(),
            Application = request.Application,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            EventType = request.EventType,
            Title = request.Title,
            Message = request.Message,
            Data = request.Data,
            CreatedBy = request.CreatedBy,
            CreatedOnUtc = _clock.UtcNow
        };

        var inserted = await _repository.InsertAsync(notification, request.Targets, cancellationToken);
        return Result<NotificationRecord>.Success(inserted);
    }

    public async Task<PagedResult<NotificationRecord>> GetNotificationsAsync(
        string application, NotificationRecipient recipient, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);
        var skip = (pageNumber - 1) * pageSize;

        var items = await _repository.ListForRecipientAsync(application, recipient, since: null, skip, pageSize, cancellationToken);
        var totalCount = await _repository.CountForRecipientAsync(application, recipient, since: null, cancellationToken);

        return new PagedResult<NotificationRecord> { Items = items, TotalCount = totalCount, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<PagedResult<NotificationRecord>> GetNotificationsForEntityAsync(
        string application, string entityType, string entityId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);
        var skip = (pageNumber - 1) * pageSize;

        var items = await _repository.ListForEntityAsync(application, entityType, entityId, skip, pageSize, cancellationToken);

        // No dedicated count query for the entity feed (this is an activity feed, not an inbox with
        // an unread badge that needs an exact total). TotalCount is exact when the page came back
        // short (nothing more to read); when a full page came back it's a lower bound — more may exist.
        var totalCount = skip + items.Count;
        return new PagedResult<NotificationRecord> { Items = items, TotalCount = totalCount, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<int> GetUnreadCountAsync(string application, NotificationRecipient recipient, CancellationToken cancellationToken = default)
    {
        var state = await _repository.GetUserStateAsync(application, recipient.UserId, cancellationToken);
        return await _repository.CountForRecipientAsync(application, recipient, state?.LastReadOnUtc, cancellationToken);
    }

    public Task<OperationResult> MarkAsReadAsync(string application, string userId, DateTimeOffset? upToUtc = null, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateApplicationAndUser(application, userId);
        if (validationError is not null) return Task.FromResult(OperationResult.Failure(validationError));

        return MarkAsReadCoreAsync(application, userId, upToUtc ?? _clock.UtcNow, cancellationToken);
    }

    public Task<OperationResult> MarkAsSyncedAsync(string application, string userId, DateTimeOffset? atUtc = null, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateApplicationAndUser(application, userId);
        if (validationError is not null) return Task.FromResult(OperationResult.Failure(validationError));

        return MarkAsSyncedCoreAsync(application, userId, atUtc ?? _clock.UtcNow, cancellationToken);
    }

    private async Task<OperationResult> MarkAsReadCoreAsync(string application, string userId, DateTimeOffset readOnUtc, CancellationToken cancellationToken)
    {
        await _repository.SetLastReadOnAsync(application, userId, readOnUtc, cancellationToken);
        return OperationResult.Success();
    }

    private async Task<OperationResult> MarkAsSyncedCoreAsync(string application, string userId, DateTimeOffset syncedOnUtc, CancellationToken cancellationToken)
    {
        await _repository.SetLastSyncedOnAsync(application, userId, syncedOnUtc, cancellationToken);
        return OperationResult.Success();
    }

    private static (int PageNumber, int PageSize) NormalizePaging(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber, "Page number must be 1 or greater.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "Page size must be 1 or greater.");

        return (pageNumber, Math.Min(pageSize, MaxPageSize));
    }

    private static ErrorInfo? ValidateApplicationAndUser(string application, string userId)
    {
        if (string.IsNullOrWhiteSpace(application)) return new ErrorInfo { Code = "APPLICATION_REQUIRED", Message = "Application is required.", Field = nameof(application) };
        if (string.IsNullOrWhiteSpace(userId)) return new ErrorInfo { Code = "USER_ID_REQUIRED", Message = "UserId is required.", Field = nameof(userId) };
        return null;
    }

    private static List<ErrorInfo> Validate(CreateNotificationRequest request)
    {
        var errors = new List<ErrorInfo>();

        if (string.IsNullOrWhiteSpace(request.Application))
            errors.Add(new ErrorInfo { Code = "APPLICATION_REQUIRED", Message = "Application is required.", Field = nameof(request.Application) });

        if (string.IsNullOrWhiteSpace(request.EventType))
            errors.Add(new ErrorInfo { Code = "EVENT_TYPE_REQUIRED", Message = "EventType is required.", Field = nameof(request.EventType) });

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add(new ErrorInfo { Code = "TITLE_REQUIRED", Message = "Title is required.", Field = nameof(request.Title) });

        if ((request.EntityType is null) != (request.EntityId is null))
            errors.Add(new ErrorInfo { Code = "ENTITY_CONTEXT_INCOMPLETE", Message = "EntityType and EntityId must both be set, or both be null.", Field = nameof(request.EntityType) });

        if (request.Targets is null || request.Targets.Count == 0)
        {
            errors.Add(new ErrorInfo { Code = "TARGETS_REQUIRED", Message = "At least one target rule is required.", Field = nameof(request.Targets) });
        }
        else
        {
            if (!request.Targets.Any(t => !t.IsExclusion))
                errors.Add(new ErrorInfo { Code = "NO_INCLUDING_TARGET", Message = "At least one non-exclusion target rule is required — a notification made only of exclusions matches nobody.", Field = nameof(request.Targets) });

            for (var i = 0; i < request.Targets.Count; i++)
            {
                var target = request.Targets[i];
                if (target.Kind == Models.NotificationTargetKind.All && target.Value is not null)
                    errors.Add(new ErrorInfo { Code = "ALL_TARGET_HAS_VALUE", Message = "A target with Kind=All must not specify a Value.", Field = $"{nameof(request.Targets)}[{i}]" });
                if (target.Kind != Models.NotificationTargetKind.All && string.IsNullOrWhiteSpace(target.Value))
                    errors.Add(new ErrorInfo { Code = "TARGET_VALUE_REQUIRED", Message = "A target with Kind=User or Kind=Group must specify a Value.", Field = $"{nameof(request.Targets)}[{i}]" });
            }
        }

        return errors;
    }
}
