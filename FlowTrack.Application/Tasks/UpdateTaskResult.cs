using FlowTrack.Application.Common;

namespace FlowTrack.Application.Tasks;

public sealed class UpdateTaskResult
{
    private UpdateTaskResult(TaskResponse? task, IReadOnlyCollection<ValidationError> errors)
    {
        Task = task;
        Errors = errors;
    }

    public TaskResponse? Task { get; }

    public IReadOnlyCollection<ValidationError> Errors { get; }

    public bool Succeeded => Errors.Count == 0;

    public static UpdateTaskResult Success(TaskResponse task) =>
        new(task, Array.Empty<ValidationError>());

    public static UpdateTaskResult Failure(IEnumerable<ValidationError> errors) =>
        new(null, errors.ToArray());
}
