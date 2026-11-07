using System.ComponentModel.DataAnnotations;

namespace FlowTrack.Api.Contracts.Authentication;

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string? Email { get; init; }

    [Required]
    public string? Password { get; init; }
}
