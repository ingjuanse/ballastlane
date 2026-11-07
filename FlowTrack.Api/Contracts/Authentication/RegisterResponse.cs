namespace FlowTrack.Api.Contracts.Authentication;

public sealed record RegisterResponse(
    Guid UserId,
    string UserName,
    string Email);
