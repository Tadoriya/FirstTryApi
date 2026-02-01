using FirstTryApi.Exceptions;
using FirstTryApi.Models;
using Microsoft.EntityFrameworkCore;
using FirstTryApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Security.Claims;

namespace FirstTryApi.Services;

// Service containing the core game logic
// Handles progression, clicks, reset and high score logic

public class GameService
{
    private readonly UserContext _context;
    private readonly ILogger<GameService> _logger;
    private readonly IHubContext<ChatHub>? _hubContext;

    private static long _cachedHighScore = 0;
    private static int _cachedHighScoreUserId = 0;

    public GameService(UserContext context, ILogger<GameService> logger, IHubContext<ChatHub>? hubContext = null)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    // Initializes a progression for a user if it does not exist
    public async Task<Progression> InitializeProgressionAsync(int userId)
    {
        bool exists = await _context.Progressions.AnyAsync(p => p.UserId == userId);
        if (exists)
            throw new GameException("Progression already exists", "PROGRESSION_EXISTS", 400);

        try
        {
            var prog = new Progression(userId);
            _context.Progressions.Add(prog);
            await _context.SaveChangesAsync();
            return prog;
        }
        catch
        {
            _logger.LogError("Failed to initialize progression for UserId {UserId}", userId);
            throw new GameException("Failed to initialize", "INITIALIZATION_FAILED", 500);
        }
    }

    // Retrieves the progression of a user
    public async Task<Progression> GetProgressionAsync(int userId)
    {
        
        var prog = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prog == null)
        {
            throw new GameException("Progression not found", "PROGRESSION_NOT_FOUND", 404);;
        }

        return prog;
    }

    // Processes a click and updates the score
    // Also checks for new global high scores
    public async Task<ClickResponse> ClickAsync(int userId)
    {
        var prog = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prog == null)
        {
            _logger.LogWarning("Click failed: No progression found - UserId {UserId}", userId);
            throw new GameException("User does not have a progression", "NO_PROGRESSION", 404);
        }

        long increment = (long)prog.Multiplier * (prog.TotalClickValue + 1);
        long newCount = (long)prog.Count + increment;

        if (newCount > int.MaxValue) prog.Count = int.MaxValue;
        else prog.Count = (int)Math.Max(0, newCount);

        if (prog.Count > prog.BestScore)
            prog.BestScore = prog.Count;

        if (prog.Count > GlobaleScore.BestScore)
        {
            GlobaleScore.BestScore = prog.Count;
            GlobaleScore.UserId = userId;

            _cachedHighScore = GlobaleScore.BestScore; 
            if (userId != _cachedHighScoreUserId)
            {
                _cachedHighScoreUserId = userId;

                var user = await _context.Users.FindAsync(userId);
                string username = user?.Username ?? "Unknown";

                if (_hubContext != null)
                {
                    await _hubContext.Clients.All.SendAsync(
                        "NewHighScore",
                        username,
                        (long)GlobaleScore.BestScore 
                    );
                }
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogDebug("Click successful: UserId {UserId}, NewCount: {Count}", userId, prog.Count);
        return new ClickResponse(prog.Count, prog.Multiplier);
    }

    // Calculates the reset cost for a user
    public async Task<ResetCostResponse> GetResetCostAsync(int userId)
    {
        var prog = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prog == null)
            throw new GameException("User has no progression", "NO_PROGRESSION", 400);

        int cost = prog.CalculateResetCost();
        return new ResetCostResponse(cost);
    }

    // Resets the progression and updates the multiplier
    public async Task<Progression> ResetProgressionAsync(int userId)
    {
        _logger.LogInformation("Progression reset attempt: UserId {UserId}", userId);

        var prog = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prog == null)
        {
            _logger.LogWarning("Progression reset failed: No progression found - UserId {UserId}", userId);
            throw new GameException("No progression", "NO_PROGRESSION", 404);
        }

        int cost = prog.CalculateResetCost();
        if (prog.Count < cost)
        {
            _logger.LogWarning(
                "Progression reset failed: Insufficient clicks - UserId {UserId}, Available: {Available}, Required: {Required}",
                userId, prog.Count, cost
            );
            throw new GameException("Not enough clicks to reset", "INSUFFICIENT_CLICKS", 400);
        }

        if (prog.Count > prog.BestScore)
            prog.BestScore = prog.Count;

        int scoreBeforeReset = prog.Count;

        prog.Count = 0;
        prog.Multiplier++;
        prog.TotalClickValue = 0;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Progression reset successfully: UserId {UserId}, NewMultiplier: {Multiplier}",
            userId, prog.Multiplier
        );

        var user = await _context.Users.FindAsync(userId);
        string username = user?.Username ?? "Unknown";

        if (_hubContext != null)
            await _hubContext.Clients.All.SendAsync("PlayerReset", username, scoreBeforeReset);

        return prog;
    }

    // Returns the best score achieved by any user
    public async Task<BestScoreResponse> GetBestScoreAsync()
    {
        
        var best = await _context.Progressions
            .OrderByDescending(p => p.BestScore)
            .FirstOrDefaultAsync();

        if (best == null  ||  best.BestScore == 0)
            throw new GameException("No progressions found", "NO_PROGRESSIONS", 404);

        return new BestScoreResponse(best.UserId, best.BestScore);
    }
}
