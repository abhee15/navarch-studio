using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;

namespace IdentityService.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var token = await _userService.AuthenticateAsync(dto, cancellationToken);

        if (token == null)
        {
            return Unauthorized("Invalid credentials");
        }

        return Ok(new { token });
    }

    [HttpPost("signup")]
    public async Task<ActionResult<string>> Signup(
        [FromBody] CreateUserDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userService.CreateUserAsync(dto, cancellationToken);

        if (user == null)
        {
            return BadRequest("User already exists or signup failed");
        }

        // Log the user in after signup
        var loginDto = new LoginDto { Email = dto.Email, Password = dto.Password };
        var token = await _userService.AuthenticateAsync(loginDto, cancellationToken);

        return Ok(new { token, user });
    }
}





