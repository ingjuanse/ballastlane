using FlowTrack.Application.Users;

namespace FlowTrack.UnitTests;

public sealed class RegisterUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_registers_user_with_hashed_password()
    {
        var repository = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        var useCase = new RegisterUserUseCase(repository, hasher);

        var result = await useCase.ExecuteAsync(
            new RegisterUserRequest("alex", "alex@example.com", "StrongPass1"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.User);
        Assert.Equal("hashed:StrongPass1", result.User.PasswordHash);
        Assert.NotEqual("StrongPass1", result.User.PasswordHash);
        Assert.Same(result.User, repository.AddedUser);
    }

    [Fact]
    public async Task ExecuteAsync_returns_duplicate_email_and_username_errors()
    {
        var repository = new FakeUserRepository
        {
            EmailExists = true,
            UserNameExists = true
        };
        var useCase = new RegisterUserUseCase(repository, new FakePasswordHasher());

        var result = await useCase.ExecuteAsync(
            new RegisterUserRequest("alex", "alex@example.com", "StrongPass1"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "email_taken");
        Assert.Contains(result.Errors, error => error.Code == "username_taken");
        Assert.Null(repository.AddedUser);
    }

    [Fact]
    public async Task ExecuteAsync_returns_all_password_validation_errors()
    {
        var repository = new FakeUserRepository();
        var useCase = new RegisterUserUseCase(repository, new FakePasswordHasher());

        var result = await useCase.ExecuteAsync(
            new RegisterUserRequest("alex", "alex@example.com", "short"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "password_length");
        Assert.Contains(result.Errors, error => error.Code == "password_uppercase");
        Assert.Contains(result.Errors, error => error.Code == "password_digit");
        Assert.Null(repository.AddedUser);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string passwordHash) => passwordHash == $"hashed:{password}";
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public bool EmailExists { get; init; }

        public bool UserNameExists { get; init; }

        public User? AddedUser { get; private set; }

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(EmailExists);

        public Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
            Task.FromResult(UserNameExists);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<IReadOnlyCollection<User>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<User>>(Array.Empty<User>());

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            AddedUser = user;
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
