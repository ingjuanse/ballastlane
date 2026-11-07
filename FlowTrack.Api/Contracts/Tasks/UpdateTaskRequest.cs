using System.ComponentModel.DataAnnotations;
using TaskStatus = FlowTrack.Domain.Enums.TaskStatus;

namespace FlowTrack.Api.Contracts.Tasks;

public sealed record UpdateTaskRequest(
    [param: Required, StringLength(200)] string? Title,
    [param: StringLength(2_000)] string? Description,
    [param: Required] TaskStatus? Status,
    [param: Required] DateTimeOffset? DueDate);
