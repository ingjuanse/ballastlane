using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FlowTrack.Application.Users;
using FlowTrack.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FlowTrack.UnitTests;

public sealed class RegisterUserUseCaseTestsWithMoq
{
    [Fact]
    public async Task ExecuteAsync_registers_user_with_hashed_password()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher
            .Setup(hasher => hasher.Hash("StrongPass1"))
            .Returns("hashed-password");
        var useCase = new RegisterUserUseCase(repository.Object, passwordHasher.Object);
        var request = new RegisterUserRequest(
            "  alex  ",
            " alex@example.com ",
            "StrongPass1");

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.UserName.Should().Be("alex");
        result.User.Email.Should().Be("alex@example.com");
        result.User.PasswordHash.Should().Be("hashed-password");
        passwordHasher.Verify(hasher => hasher.Hash("StrongPass1"), Times.Once);
        repository.Verify(
            repository => repository.AddAsync(
                It.Is<User>(user =>
                    user.UserName == "alex" &&
                    user.Email == "alex@example.com" &&
                    user.PasswordHash == "hashed-password"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_returns_duplicate_errors_without_hashing_or_persisting()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(repository => repository.ExistsByEmailAsync("alex@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository
            .Setup(repository => repository.ExistsByUserNameAsync("alex", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var passwordHasher = new Mock<IPasswordHasher>();
        var useCase = new RegisterUserUseCase(repository.Object, passwordHasher.Object);

        // Act
        var result = await useCase.ExecuteAsync(
            new RegisterUserRequest("alex", "alex@example.com", "StrongPass1"));

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Select(error => error.Code).Should().Contain(new[]
        {
            "email_taken",
            "username_taken"
        });
        passwordHasher.Verify(hasher => hasher.Hash(It.IsAny<string>()), Times.Never);
        repository.Verify(
            repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_errors_without_repository_access()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var useCase = new RegisterUserUseCase(repository.Object, passwordHasher.Object);

        // Act
        var result = await useCase.ExecuteAsync(
            new RegisterUserRequest(null, "invalid-email", "short"));

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Select(error => error.Code).Should().Contain(new[]
        {
            "username_required",
            "email_invalid",
            "password_length",
            "password_uppercase",
            "password_digit"
        });
        repository.VerifyNoOtherCalls();
        passwordHasher.VerifyNoOtherCalls();
    }
}

public sealed class LoginUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_returns_token_for_valid_credentials()
    {
        // Arrange
        var user = User.Create("alex", "alex@example.com", "hashed-password");
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(repository => repository.GetByEmailAsync("alex@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher
            .Setup(hasher => hasher.Verify("StrongPass1", "hashed-password"))
            .Returns(true);
        var tokenGenerator = new Mock<ITokenGenerator>();
        tokenGenerator
            .Setup(generator => generator.Generate(user))
            .Returns("access-token");
        var useCase = new LoginUseCase(repository.Object, passwordHasher.Object, tokenGenerator.Object);

        // Act
        var result = await useCase.ExecuteAsync(
            new LoginRequest(" alex@example.com ", "StrongPass1"));

        // Assert
        result.Succeeded.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        result.UserId.Should().Be(user.Id);
        passwordHasher.Verify(hasher => hasher.Verify("StrongPass1", "hashed-password"), Times.Once);
        tokenGenerator.Verify(generator => generator.Generate(user), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_returns_invalid_credentials_when_user_does_not_exist()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(repository => repository.GetByEmailAsync("missing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var passwordHasher = new Mock<IPasswordHasher>();
        var tokenGenerator = new Mock<ITokenGenerator>();
        var useCase = new LoginUseCase(repository.Object, passwordHasher.Object, tokenGenerator.Object);

        // Act
        var result = await useCase.ExecuteAsync(
            new LoginRequest("missing@example.com", "StrongPass1"));

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.Code == "invalid_credentials");
        passwordHasher.VerifyNoOtherCalls();
        tokenGenerator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_returns_invalid_credentials_when_password_is_incorrect()
    {
        // Arrange
        var user = User.Create("alex", "alex@example.com", "hashed-password");
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(repository => repository.GetByEmailAsync("alex@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher
            .Setup(hasher => hasher.Verify("WrongPass1", "hashed-password"))
            .Returns(false);
        var tokenGenerator = new Mock<ITokenGenerator>();
        var useCase = new LoginUseCase(repository.Object, passwordHasher.Object, tokenGenerator.Object);

        // Act
        var result = await useCase.ExecuteAsync(
            new LoginRequest("alex@example.com", "WrongPass1"));

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.Code == "invalid_credentials");
        tokenGenerator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_returns_invalid_credentials_without_repository_access_for_missing_input()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var tokenGenerator = new Mock<ITokenGenerator>();
        var useCase = new LoginUseCase(repository.Object, passwordHasher.Object, tokenGenerator.Object);

        // Act
        var result = await useCase.ExecuteAsync(new LoginRequest(" ", null));

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.Code == "invalid_credentials");
        repository.VerifyNoOtherCalls();
        passwordHasher.VerifyNoOtherCalls();
        tokenGenerator.VerifyNoOtherCalls();
    }
}

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void Generate_creates_signed_token_with_expected_claims_and_configuration()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "flowtrack-tests",
            Audience = "flowtrack-api-tests",
            Secret = "test-secret-with-at-least-32-characters-long",
            ExpirationMinutes = 30
        };
        var generator = new JwtTokenGenerator(Options.Create(settings));
        var user = User.Create("alex", "alex@example.com", "hashed-password");

        // Act
        var token = generator.Generate(user);
        var handler = new JwtSecurityTokenHandler();
        var parsedToken = handler.ReadJwtToken(token);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        parsedToken.Issuer.Should().Be(settings.Issuer);
        parsedToken.Audiences.Should().ContainSingle().Which.Should().Be(settings.Audience);
        parsedToken.Claims.Should().Contain(claim =>
            claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == user.Id.ToString());
        parsedToken.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.NameIdentifier && claim.Value == user.Id.ToString());
        parsedToken.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.Name && claim.Value == user.UserName);
        parsedToken.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.Email && claim.Value == user.Email);
        parsedToken.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.Role && claim.Value == "User");
        parsedToken.Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
        parsedToken.ValidTo.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes),
            precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Generate_rejects_null_user()
    {
        // Arrange
        var settings = new JwtSettings
        {
            Issuer = "flowtrack-tests",
            Audience = "flowtrack-api-tests",
            Secret = "test-secret-with-at-least-32-characters-long"
        };
        var generator = new JwtTokenGenerator(Options.Create(settings));

        // Act
        var action = () => generator.Generate(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }
}
