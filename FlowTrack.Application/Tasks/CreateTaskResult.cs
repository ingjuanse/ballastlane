using FlowTrack.Application.Common;

namespace FlowTrack.Application.Tasks;

public sealed class CreateTaskResult
{
    private CreateTaskResult(TaskResponse? task, IReadOnlyCollection<ValidationError> errors)
    {
        Task = task;
        Errors = errors;
    }

    public TaskResponse? Task { get; }

    public IReadOnlyCollection<ValidationError> Errors { get; }

    public bool Succeeded => Errors.Count == 0;

    public static CreateTaskResult Success(TaskResponse task) =>
        new(task, Array.Empty<ValidationError>());

    public static CreateTaskResult Failure(IEnumerable<ValidationError> errors) =>
        new(null, errors.ToArray());
}
