using FlowTrack.Application.Common;
using FlowTrack.Domain.Entities;

namespace FlowTrack.Application.Users;

public sealed class RegisterUserResult
{
    private RegisterUserResult(User? user, IReadOnlyCollection<ValidationError> errors)
    {
        User = user;
        Errors = errors;
    }

    public User? User { get; }

    public IReadOnlyCollection<ValidationError> Errors { get; }

    public bool Succeeded => Errors.Count == 0;

    public static RegisterUserResult Success(User user) =>
        new(user, Array.Empty<ValidationError>());

    public static RegisterUserResult Failure(IEnumerable<ValidationError> errors) =>
        new(null, errors.ToArray());
}
