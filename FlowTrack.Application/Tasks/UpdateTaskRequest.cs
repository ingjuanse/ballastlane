using TaskStatus = FlowTrack.Domain.Enums.TaskStatus;

namespace FlowTrack.Application.Tasks;

public sealed record UpdateTaskRequest(
    string? Title,
    string? Description,
    TaskStatus? Status,
    DateTimeOffset? DueDate);
