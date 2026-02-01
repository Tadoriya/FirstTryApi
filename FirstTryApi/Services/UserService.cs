using FirstTryApi.Exceptions;
using FirstTryApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FirstTryApi.Services;

public class UserService
{
    private readonly UserContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtService _jwtService;
    private readonly ILogger<UserService> _logger;
    private readonly GameService? _gameService;

    public UserService(
        UserContext context,
        IPasswordHasher<User> passwordHasher,
        JwtService jwtService,
        ILogger<UserService> logger,
        GameService? gameService=null)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _logger = logger;
        _gameService = gameService;
    }

    private static UserPublic ToPublic(User u) => new UserPublic
    {
        Id = u.Id,
        Username = u.Username,
        Role = u.Role
    };


    public async Task<List<UserPublic>> GetAllAsync()
    {
        return await _context.Users
            .Select(u => ToPublic(u))
            .ToListAsync();
    }

    public async Task<UserPublic> GetByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new GameException("User not found", "USER_NOT_FOUND", 404);

        return ToPublic(user);
    }

    public async Task<List<UserPublic>> GetAllAdminsAsync()
    {
        return await _context.Users
            .Where(u => u.Role == UserRole.Admin)
            .Select(u => ToPublic(u))
            .ToListAsync();
    }

    public async Task<List<UserPublic>> SearchAsync(string name)
    {
        return await _context.Users
            .Where(u => u.Username.Contains(name))
            .Select(u => ToPublic(u))
            .ToListAsync();
    }

    public async Task<(string Token, UserPublic User)> RegisterAsync(UserPass info)
    {
        _logger.LogInformation("Register attempt: {Username}", info.Username);

        bool exists = await _context.Users.AnyAsync(u => u.Username == info.Username);
       if (exists)
       {
        _logger.LogWarning("Registration failed: Username already exists {Username}", info.Username);
        throw new GameException("Username already exists", "USERNAME_EXISTS", 400);
        }

        bool adminExists = await _context.Users.AnyAsync(u => u.Role == UserRole.Admin);

        var user = new User
        {
            Username = info.Username,
            Role = adminExists ? UserRole.User : UserRole.Admin
        };

        user.Password = _passwordHasher.HashPassword(user, info.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        if (_gameService != null)
        {
            await _gameService.InitializeProgressionAsync(user.Id);
        }
        else
        {
            _context.Progressions.Add(new Progression(user.Id));
            await _context.SaveChangesAsync();
        }   var token = _jwtService.GenerateToken(user);
        _logger.LogInformation("Register success: {Username} ({Role})", user.Username, user.Role);
        
        return (Token: token, User: ToPublic(user));
    }

    public async Task<(string Token, UserPublic User)> LoginAsync(UserPass info)
    {
        _logger.LogInformation("Login attempt: {Username}", info.Username);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == info.Username);
        if (user == null)
            throw new GameException("User not found", "USER_NOT_FOUND", 404);

        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, info.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed: Invalid password for {Username}",info.Username);
            throw new GameException("Invalid password", "INVALID_PASSWORD", 401);
        }

        var token = _jwtService.GenerateToken(user);
        _logger.LogInformation("Login success: {Username}", user.Username);

        return (Token: token, User: ToPublic(user));

    }

    public async Task<UserPublic> UpdateUserAsync(int id, UserUpdate newone)
    {
        _logger.LogInformation("Update user attempt: {UserId}", id);

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new GameException("User not found", "USER_NOT_FOUND", 404);

        user.Username = newone.Username;
        user.Password = _passwordHasher.HashPassword(user, newone.Password);
        user.Role = newone.Role;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Update user success: {UserId}", id);

        return ToPublic(user);
    }

    public async Task<object> DeleteUserAsync(int id)
    {
        _logger.LogInformation("Delete user attempt: {UserId}", id);

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new GameException("User not found", "USER_NOT_FOUND", 404);

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Delete user success: {UserId}", id);
        return new { message = "User deleted succesfully" };
    }
}
