using TaskStatus = FlowTrack.Domain.Enums.TaskStatus;

namespace FlowTrack.Application.Tasks;

public sealed record TaskQuery(
    int PageNumber = 1,
    int PageSize = 20,
    TaskStatus? Status = null,
    TaskSortField SortBy = TaskSortField.CreatedUtc,
    bool SortDescending = true,
    string? Search = null);
