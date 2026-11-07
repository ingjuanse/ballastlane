using FlowTrack.Application.Common;

namespace FlowTrack.Application.Tasks;

public sealed class GetTasksResult
{
    private GetTasksResult(PagedResult<TaskResponse>? page, IReadOnlyCollection<ValidationError> errors)
    {
        Page = page;
        Errors = errors;
    }

    public PagedResult<TaskResponse>? Page { get; }

    public IReadOnlyCollection<ValidationError> Errors { get; }

    public bool Succeeded => Errors.Count == 0;

    public static GetTasksResult Success(PagedResult<TaskResponse> page) =>
        new(page, Array.Empty<ValidationError>());

    public static GetTasksResult Failure(IEnumerable<ValidationError> errors) =>
        new(null, errors.ToArray());
}
