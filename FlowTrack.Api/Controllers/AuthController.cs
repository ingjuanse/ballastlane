using FlowTrack.Api.Contracts.Authentication;
using FlowTrack.Application.Users;
using Microsoft.AspNetCore.Mvc;
using ApiLoginRequest = FlowTrack.Api.Contracts.Authentication.LoginRequest;

namespace FlowTrack.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(
    RegisterUserUseCase registerUserUseCase,
    LoginUseCase loginUseCase) : ControllerBase
{
    /// <summary>Registers a new FlowTrack user.</summary>
    /// <remarks>Passwords must be at least eight characters and include uppercase, lowercase, and numeric characters.</remarks>
    /// <response code="201">The user was registered.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="409">The email address or username is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await registerUserUseCase.ExecuteAsync(
            new Application.Users.RegisterUserRequest(request.UserName, request.Email, request.Password),
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code is "email_taken" or "username_taken"))
            {
                return Conflict(new { errors = result.Errors });
            }

            return BadRequest(new ValidationProblemDetails(
                result.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray())));
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new RegisterResponse(result.User!.Id, result.User.UserName, result.User.Email));
    }

    /// <summary>Authenticates a user and returns a JWT access token.</summary>
    /// <response code="200">The credentials are valid and a token was issued.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">The email or password is incorrect.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] ApiLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginUseCase.ExecuteAsync(
            new Application.Users.LoginRequest(request.Email, request.Password),
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code == "invalid_credentials"))
            {
                return Unauthorized(new { errors = result.Errors });
            }

            return BadRequest(new ValidationProblemDetails(
                result.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray())));
        }

        return Ok(new AuthenticationResponse(result.AccessToken!, result.UserId!.Value));
    }
}
