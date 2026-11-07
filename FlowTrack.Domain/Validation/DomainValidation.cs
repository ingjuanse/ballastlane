using System.Net.Mail;
using FlowTrack.Domain.Exceptions;

namespace FlowTrack.Domain.Validation;

internal static class DomainValidation
{
    public static string Required(string? value, string parameterName, int maximumLength)
    {
        var normalizedValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException($"{parameterName} cannot exceed {maximumLength} characters.", parameterName);
        }

        return normalizedValue;
    }

    public static string Email(string? value, string parameterName)
    {
        var normalizedValue = Required(value, parameterName, 254);

        try
        {
            var address = new MailAddress(normalizedValue);
            if (!string.Equals(address.Address, normalizedValue, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch (FormatException)
        {
            throw new ArgumentException("A valid email address is required.", parameterName);
        }

        return normalizedValue;
    }

    public static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        return value;
    }

    public static void FutureOrPresent(DateTimeOffset? value, string parameterName)
    {
        if (value is not null && value.Value < DateTimeOffset.UtcNow)
        {
            throw new DomainRuleViolationException($"{parameterName} cannot be in the past.");
        }
    }
}
