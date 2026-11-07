using FlowTrack.Application.Abstractions;

namespace FlowTrack.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string passwordHash)
    {
        return !string.IsNullOrWhiteSpace(password)
            && !string.IsNullOrWhiteSpace(passwordHash)
            && BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
