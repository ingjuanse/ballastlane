namespace FlowTrack.Api.Contracts.Authentication;

public sealed record AuthenticationResponse(
    string AccessToken,
    Guid UserId);
