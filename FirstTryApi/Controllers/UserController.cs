using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using FirstTryApi.Models;
using FirstTryApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


namespace FirstTryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtService _jwtService;

    public UserController(UserContext context, IPasswordHasher<User> motdepasse, JwtService jwtservice)
    {
        _context = context;
        _passwordHasher = motdepasse;
        _jwtService = jwtservice;
    }


    private static UserPublic ToPublic(User u)
    {
        return new UserPublic
        {
            Id = u.Id,
            Username = u.Username,
            Role = u.Role
        };
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return null;
        }
        return userId;
    }



    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserPublic>> GetById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new ErrorResponse("User not found", "USER_NOT_FOUND"));
        return ToPublic(user);

    }

    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> Login([FromBody] UserPass info)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == info.Username);
        if (user == null)
            return NotFound(new ErrorResponse("User not found", "USER_NOT_FOUND"));
        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, info.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new ErrorResponse("Invalid password", "INVALID_PASSWORD"));
        var token = _jwtService.GenerateToken(user);

        return Ok(new {token=token, user=ToPublic(user)});

    }



    [HttpPost("Register")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> Register([FromBody] UserPass info)
    {
        try
        {
            
            if (await _context.Users.AnyAsync(u => u.Username == info.Username))
                return BadRequest(new ErrorResponse("Username already exists", "USERNAME_EXISTS"));

            var admin = await _context.Users.AnyAsync(u => u.Role == UserRole.Admin);

            var user = new User
            {
                Username = info.Username,
                Role = admin ? UserRole.User : UserRole.Admin
            };
            user.Password = _passwordHasher.HashPassword(user, info.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);

            return Ok(new { token = token, user = ToPublic(user) });
        }
        catch 
        {
            return BadRequest(new ErrorResponse("Registration failed", "REGISTRATION_FAILED"));
        }
    }

    [HttpPut("User/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserPublic>> UpdateUser(int id, UserUpdate newone)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new ErrorResponse("User not found", "USER_NOT_FOUND"));
        user.Username=newone.Username;
        user.Password= _passwordHasher.HashPassword(user, newone.Password);
        user.Role=newone.Role;
        await _context.SaveChangesAsync();
        return Ok(ToPublic(user));
    }

    [HttpDelete("User/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new ErrorResponse("User not found", "USER_NOT_FOUND"));
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok(new {message =  "User deleted succesfully" } );
    }
    
 
    
   [HttpGet("All")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<UserPublic>>> GetAll()
   {
       var users= await _context.Users.Select(u => ToPublic(u)).ToListAsync();
       return Ok(users);

   }

    [HttpGet("AllAdmin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserPublic>>> GetAllAdmins()
    {
        var admins = await _context.Users.Where(u => u.Role==UserRole.Admin).Select(u => ToPublic(u)).ToListAsync();
        return Ok(admins);

    }

    [HttpGet("Search/{name}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<UserPublic>>> GetByName(string name)
    {
        var found = await _context.Users.Where(u => u.Username.Contains(name)).Select(u => ToPublic(u)).ToListAsync();
        return Ok(found);
    }

}
