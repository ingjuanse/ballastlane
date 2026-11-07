namespace FlowTrack.Application.Users;

public sealed record LoginRequest(string? Email, string? Password);
