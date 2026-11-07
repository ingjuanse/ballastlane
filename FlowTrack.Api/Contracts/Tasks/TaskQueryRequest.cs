using System.ComponentModel.DataAnnotations;
using TaskStatus = FlowTrack.Domain.Enums.TaskStatus;

namespace FlowTrack.Api.Contracts.Tasks;

public sealed record TaskQueryRequest(
    [param: Range(1, int.MaxValue)] int PageNumber = 1,
    [param: Range(1, 100)] int PageSize = 20,
    TaskStatus? Status = null,
    TaskSortField SortBy = TaskSortField.CreatedUtc,
    bool SortDescending = true,
    [param: StringLength(200)] string? Search = null);

public enum TaskSortField
{
    CreatedUtc = 1,
    DueDate = 2,
    Title = 3,
    Status = 4
}
