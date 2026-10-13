using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VoucherHub.Api.Contracts.Auth;
using VoucherHub.Application.Identity;

namespace VoucherHub.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string GenericRegistrationMessage =
        "If the email is eligible, verification instructions will be sent.";

    private readonly IRegistrationService _registrationService;
    private readonly IAuthenticationService _authenticationService;

    public AuthController(
        IRegistrationService registrationService,
        IAuthenticationService authenticationService)
    {
        _registrationService = registrationService;
        _authenticationService = authenticationService;
    }

    [HttpPost("register")]
    [EnableRateLimiting("registration")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            ModelState.AddModelError(
                nameof(request.DisplayName),
                "Display name must not be blank.");
            return ValidationProblem(ModelState);
        }

        await _registrationService.RegisterAsync(
            new RegisterUserCommand(
                request.Email,
                request.Password,
                request.DisplayName),
            cancellationToken);

        return Accepted(new { message = GenericRegistrationMessage });
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting("registration")]
    public async Task<IActionResult> ResendVerification(
        ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await _registrationService.ResendVerificationAsync(
            request.Email,
            cancellationToken);

        return Accepted(new { message = GenericRegistrationMessage });
    }

    [HttpPost("verify-email")]
    [EnableRateLimiting("registration")]
    public async Task<IActionResult> VerifyEmail(
        VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _registrationService.VerifyEmailAsync(
            request.Token,
            cancellationToken);

        return result switch
        {
            VerifyEmailResult.Success => Ok(new
            {
                message = "Email verification succeeded."
            }),
            VerifyEmailResult.EmailAlreadyRegistered => Conflict(new
            {
                message = "The email is already registered."
            }),
            _ => BadRequest(new
            {
                message = "The verification token is invalid or expired."
            })
        };
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationService.LoginAsync(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        return result.Status switch
        {
            LoginStatus.Success => Ok(new
            {
                accessToken = result.AccessToken,
                tokenType = "Bearer",
                expiresAtUtc = result.ExpiresAtUtc
            }),
            LoginStatus.Blocked => StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "The account is blocked." }),
            _ => Unauthorized(new
            {
                message = "Invalid email or password."
            })
        };
    }
}
