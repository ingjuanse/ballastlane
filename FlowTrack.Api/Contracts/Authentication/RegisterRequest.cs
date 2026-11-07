using System.ComponentModel.DataAnnotations;

namespace FlowTrack.Api.Contracts.Authentication;

public sealed class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string? UserName { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public string? Email { get; init; }

    [Required, MinLength(8)]
    public string? Password { get; init; }
}
