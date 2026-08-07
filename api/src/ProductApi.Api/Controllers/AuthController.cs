using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductApi.Api.Dtos;
using ProductApi.Infrastructure.Auth;

namespace ProductApi.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController(DemoUserStore userStore, JwtTokenService tokenService) : ControllerBase
{
    /// <summary>
    /// Authenticates a demo user and returns a signed JWT.
    /// Demo credentials: admin/admin123 (ADMIN), user/user123 (USER).
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequestDto request)
    {
        var roles = userStore.Authenticate(request.Username, request.Password);
        if (roles is null)
            return Unauthorized(new ErrorResponseDto(DateTimeOffset.UtcNow.ToString("O"), 401, "Unauthorized", "Invalid username or password"));

        var (token, expiresIn) = tokenService.IssueToken(request.Username, roles);
        return Ok(new LoginResponseDto(token, "Bearer", expiresIn, request.Username, roles));
    }
}
