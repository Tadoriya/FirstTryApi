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
    private readonly PasswordHasher<User> _passwordHasher;
    public UserController(UserContext context, PasswordHasher<User> motdepasse)
    {
          _context = context;
        _passwordHasher=motdepasse;
    }
    

    private static UserPublic ToPublic(User u)
    {
        return new UserPublic
        {
            Id = u.Id,
            Pseudo = u.Pseudo,
            Role = u.Role
        };
    }

    [HttpGet("Debug")]
    public async Task<ActionResult<IEnumerable<User>>> Debug()
    {
        return await _context.Users.ToListAsync();
    }
   
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserPublic>> GetById(int id)
    {
        var user= await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound($"Aucun utilisateur trouvé avec l'id {id}");
        return ToPublic(user);

    }

    [HttpPost("Login")]
    public async Task<ActionResult<UserPublic>> Login(UserInfo info)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Pseudo == info.Pseudo);
        if (user == null)
            return NotFound("Pseudo incorrect");
        var result = _passwordHasher.VerifyHashedPassword(user, user.MotdePasse, info.MotdePasse);
        if(result==PasswordVerificationResult.Success)
            return ToPublic(user);
        else
            return Unauthorized("Mdp incorrect");


    }



    [HttpPost("Register")]
   public async Task<ActionResult<UserInfo>> Register(UserInfo newUser)
   {
        if (await _context.Users.AnyAsync(u => u.Pseudo == newUser.Pseudo))
            return Conflict("deja inscrit :c");
       var user = new User
       {
           Pseudo = newUser.Pseudo,
           Role = UserRole.User
       };
        user.MotdePasse= _passwordHasher.HashPassword(user, newUser.MotdePasse);
       _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById),new {id =user.Id}, ToPublic(user)) ;

   }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserPublic>> UpdateUser(int id, UserUpdate newone)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound($"Utilisateur avec l'id ={id} n'est pas trouvé :C");
        user.Pseudo=newone.Pseudo;
        user.MotdePasse=newone.MotdePasse;
        user.Role=newone.Role;
        await _context.SaveChangesAsync();
        return Ok(ToPublic(user));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound($"Utilisateur avec l'id ={id} n'est pas trouvé :C");
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
        var found = await _context.Users.Where(u => u.Pseudo.Contains(name)).Select(u => ToPublic(u)).ToListAsync();
        return Ok(found);
    }

}
