using FlowTrack.Application.Common;

namespace FlowTrack.Application.Users;

public sealed record AuthenticationResult(
    bool Succeeded,
    string? AccessToken,
    Guid? UserId,
    IReadOnlyCollection<ValidationError> Errors)
{
    public static AuthenticationResult Success(string token, Guid userId) =>
        new(true, token, userId, Array.Empty<ValidationError>());

    public static AuthenticationResult Failure(params ValidationError[] errors) =>
        new(false, null, null, errors);
}
