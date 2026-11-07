using TaskStatus = FlowTrack.Domain.Enums.TaskStatus;

namespace FlowTrack.Application.Tasks;

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string Description,
    TaskStatus Status,
    DateTimeOffset DueDate,
    Guid UserId,
    DateTimeOffset CreatedUtc);
