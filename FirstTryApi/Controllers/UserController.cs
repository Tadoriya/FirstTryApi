using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using FirstTryApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace FirstTryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserController(UserContext context, IPasswordHasher<User> motdepasse)
    {
        _context = context;
        _passwordHasher = motdepasse;
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

    [HttpGet("Debug")]
    public async Task<ActionResult<IEnumerable<User>>> Debug()
    {
        return await _context.Users.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserPublic>> GetById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new ErrorResponse("User not found", "USER_NOT_FOUND"));
        return ToPublic(user);

    }

    [HttpPost("Login")]
    public async Task<ActionResult<UserPublic>> Login([FromBody] UserPass info)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == info.Username);
        if (user == null)
            return NotFound(new ErrorResponse("User not found", "USER_NOT_FOUND"));
        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, info.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new ErrorResponse("Invalid password", "INVALID_PASSWORD"));      

        return ToPublic(user);

    }



    [HttpPost("Register")]
    public async Task<ActionResult<UserPublic>> Register([FromBody] UserPass info)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Username == info.Username))
                return BadRequest(new ErrorResponse("Username already exists", "USERNAME_EXISTS"));

            var user = new User
            {
                Username = info.Username,
                Role = UserRole.User
            };
            user.Password = _passwordHasher.HashPassword(user, info.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(ToPublic(user));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return BadRequest(new ErrorResponse("Registration failed", "REGISTRATION_FAILED"));
        }
    }

    [HttpPut("User/{id}")]
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
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new ErrorResponse("User not found", "USER_NOT_FOUND"));
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
 
    
   [HttpGet("All")]
   public async Task<ActionResult<IEnumerable<UserPublic>>> GetAll()
   {
       var users= await _context.Users.Select(u => ToPublic(u)).ToListAsync();
       return Ok(users);

   }

    [HttpGet("AllAdmin")]
    public async Task<ActionResult<IEnumerable<UserPublic>>> GetAllAdmins()
    {
        var admins = await _context.Users.Where(u => u.Role==UserRole.Admin).Select(u => ToPublic(u)).ToListAsync();
        return Ok(admins);

    }

    [HttpGet("Search/{name}")]
    public async Task<ActionResult<IEnumerable<UserPublic>>> GetByName(string name)
    {
        var found = await _context.Users.Where(u => u.Username.Contains(name)).Select(u => ToPublic(u)).ToListAsync();
        return Ok(found);
    }

}
