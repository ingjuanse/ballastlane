using TaskStatus = FlowTrack.Domain.Enums.TaskStatus;

namespace FlowTrack.Api.Contracts.Tasks;

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string Description,
    TaskStatus Status,
    DateTimeOffset DueDate,
    Guid UserId,
    DateTimeOffset CreatedUtc);

public sealed record PagedTaskResponse(
    IReadOnlyCollection<TaskResponse> Items,
    int PageNumber,
    int PageSize,
    long TotalCount);
