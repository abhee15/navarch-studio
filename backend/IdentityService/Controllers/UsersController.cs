using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using IdentityService.Services;
using Shared.Services;

namespace IdentityService.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly IJwtService _jwtService;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger,
        IJwtService jwtService)
    {
        _userService = userService;
        _logger = logger;
        _jwtService = jwtService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        // Get user ID from JWT claims
        var userId = _jwtService.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(user);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(string id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(
        [FromBody] CreateUserDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userService.CreateUserAsync(dto, cancellationToken);

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            user);
    }
}





