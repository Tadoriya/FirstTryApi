using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FirstTryApi.Models;
using FirstTryApi.Services;

namespace FirstTryApi.Controllers;

// Controller responsible for user management and authentication
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserPublic>> GetById(int id)
        => Ok(await _userService.GetByIdAsync(id));


    // Authenticates a user and returns a JWT token
    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> Login([FromBody] UserPass info)
    {
        var result = await _userService.LoginAsync(info);
        return Ok(new { token = result.Token, user = result.User });
    }


    // Registers a new user and returns a JWT token
    [HttpPost("Register")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> Register([FromBody] UserPass info)
    {
        var result = await _userService.RegisterAsync(info);
        return Ok(new { token = result.Token, user = result.User });
    }


    [HttpPut("User/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserPublic>> UpdateUser(int id, [FromBody] UserUpdate newone)
        => Ok(await _userService.UpdateUserAsync(id, newone));

    [HttpDelete("User/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
        => Ok(await _userService.DeleteUserAsync(id));


    // Returns all registered users
    [HttpGet("All")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<UserPublic>>> GetAll()
        => Ok(await _userService.GetAllAsync());

    [HttpGet("AllAdmin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserPublic>>> GetAllAdmins()
        => Ok(await _userService.GetAllAdminsAsync());


    // Searches users by username
    [HttpGet("Search/{name}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<UserPublic>>> GetByName(string name)
        => Ok(await _userService.SearchAsync(name));
}
