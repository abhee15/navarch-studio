using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using IdentityService.Services;

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
}





