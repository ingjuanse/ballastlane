using System.ComponentModel.DataAnnotations;

namespace FlowTrack.Api.Contracts.Tasks;

public sealed record CreateTaskRequest(
    [param: Required, StringLength(200)] string? Title,
    [param: StringLength(2_000)] string? Description,
    [param: Required] DateTimeOffset? DueDate);
