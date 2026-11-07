using FlowTrack.Application.Abstractions;
using FlowTrack.Application.Common;

namespace FlowTrack.Application.Users;

public sealed class LoginUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator)
{
    public async Task<AuthenticationResult> ExecuteAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return AuthenticationResult.Failure(
                new ValidationError("invalid_credentials", "Email and password are required."));
        }

        var user = await userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return AuthenticationResult.Failure(
                new ValidationError("invalid_credentials", "The email or password is incorrect."));
        }

        return AuthenticationResult.Success(tokenGenerator.Generate(user), user.Id);
    }
}
