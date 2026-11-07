namespace FlowTrack.Application.Tasks;

public sealed record CreateTaskRequest(
    string? Title,
    string? Description,
    DateTimeOffset? DueDate,
    Guid? UserId);
