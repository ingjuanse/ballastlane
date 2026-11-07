using FlowTrack.Domain.Validation;

namespace FlowTrack.Domain.Entities;

public sealed class User
{
    private User(Guid id, string userName, string email, string passwordHash)
    {
        Id = id;
        UserName = userName;
        Email = email;
        PasswordHash = passwordHash;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public string UserName { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTimeOffset CreatedUtc { get; }

    public static User Create(string userName, string email, string passwordHash)
    {
        var normalizedUserName = DomainValidation.Required(userName, nameof(userName), 100);
        if (normalizedUserName.Length < 3)
        {
            throw new ArgumentException("User name must contain at least 3 characters.", nameof(userName));
        }

        return new User(
            Guid.NewGuid(),
            normalizedUserName,
            DomainValidation.Email(email, nameof(email)),
            DomainValidation.Required(passwordHash, nameof(passwordHash), 500));
    }

    public void UpdateProfile(string userName, string email)
    {
        var normalizedUserName = DomainValidation.Required(userName, nameof(userName), 100);
        if (normalizedUserName.Length < 3)
        {
            throw new ArgumentException("User name must contain at least 3 characters.", nameof(userName));
        }

        UserName = normalizedUserName;
        Email = DomainValidation.Email(email, nameof(email));
    }

    public void ChangePasswordHash(string passwordHash)
    {
        PasswordHash = DomainValidation.Required(passwordHash, nameof(passwordHash), 500);
    }
}
