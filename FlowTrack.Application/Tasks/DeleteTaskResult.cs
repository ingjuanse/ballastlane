using FlowTrack.Application.Common;

namespace FlowTrack.Application.Tasks;

public sealed class DeleteTaskResult
{
    private DeleteTaskResult(bool succeeded, IReadOnlyCollection<ValidationError> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }

    public IReadOnlyCollection<ValidationError> Errors { get; }

    public static DeleteTaskResult Success() =>
        new(true, Array.Empty<ValidationError>());

    public static DeleteTaskResult Failure(IEnumerable<ValidationError> errors) =>
        new(false, errors.ToArray());
}
