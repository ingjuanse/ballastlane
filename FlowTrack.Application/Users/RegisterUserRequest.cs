namespace FlowTrack.Application.Users;

public sealed record RegisterUserRequest(
    string? UserName,
    string? Email,
    string? Password);
