using System.Net.Mail;
using FlowTrack.Application.Abstractions;
using FlowTrack.Application.Common;
using FlowTrack.Domain.Entities;

namespace FlowTrack.Application.Users;

public sealed class RegisterUserUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
{
    public async Task<RegisterUserResult> ExecuteAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return RegisterUserResult.Failure(errors);
        }

        var normalizedUserName = request.UserName!.Trim();
        var normalizedEmail = request.Email!.Trim();

        if (await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            errors.Add(new ValidationError("email_taken", "The email address is already registered."));
        }

        if (await userRepository.ExistsByUserNameAsync(normalizedUserName, cancellationToken))
        {
            errors.Add(new ValidationError("username_taken", "The username is already registered."));
        }

        if (errors.Count > 0)
        {
            return RegisterUserResult.Failure(errors);
        }

        var passwordHash = passwordHasher.Hash(request.Password!);
        var user = User.Create(normalizedUserName, normalizedEmail, passwordHash);
        await userRepository.AddAsync(user, cancellationToken);

        return RegisterUserResult.Success(user);
    }

    private static List<ValidationError> Validate(RegisterUserRequest request)
    {
        var errors = new List<ValidationError>();

        var userName = request.UserName?.Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            errors.Add(new ValidationError("username_required", "Username is required."));
        }
        else if (userName.Length < 3 || userName.Length > 100)
        {
            errors.Add(new ValidationError("username_length", "Username must be between 3 and 100 characters."));
        }

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(new ValidationError("email_required", "Email is required."));
        }
        else if (email.Length > 254 || !IsValidEmail(email))
        {
            errors.Add(new ValidationError("email_invalid", "A valid email address is required."));
        }

        var password = request.Password;
        if (string.IsNullOrEmpty(password))
        {
            errors.Add(new ValidationError("password_required", "Password is required."));
        }
        else
        {
            if (password.Length < 8)
            {
                errors.Add(new ValidationError("password_length", "Password must be at least 8 characters long."));
            }

            if (!password.Any(char.IsUpper))
            {
                errors.Add(new ValidationError("password_uppercase", "Password must contain an uppercase letter."));
            }

            if (!password.Any(char.IsLower))
            {
                errors.Add(new ValidationError("password_lowercase", "Password must contain a lowercase letter."));
            }

            if (!password.Any(char.IsDigit))
            {
                errors.Add(new ValidationError("password_digit", "Password must contain a number."));
            }
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
