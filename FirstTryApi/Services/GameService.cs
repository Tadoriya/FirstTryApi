using FirstTryApi.Exceptions;
using FirstTryApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using FirstTryApi.Hubs;

namespace FirstTryApi.Services;

public class GameService
{
    private readonly UserContext _context;
    private readonly ILogger<GameService> _logger;
    private readonly IHubContext<ChatHub> _hubContext;

    public GameService(UserContext context, ILogger<GameService> logger, IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task<Progression> InitializeProgressionAsync(int userId)
    {
        bool exists = await _context.Progressions.AnyAsync(p => p.UserId == userId);
        if (exists)
            throw new GameException("User has already a progression", "PROGRESSION_EXISTS", 400);

        var prog = new Progression(userId);
        _context.Progressions.Add(prog);
        await _context.SaveChangesAsync();

        return prog;
    }

    public async Task<Progression> GetProgressionAsync(int userId)
    {
        var prog = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prog == null)
            throw new GameException("Progression not found", "PROGRESSION_NOT_FOUND", 404);

        return prog;
    }

    public async Task<ClickResponse> ClickAsync(int userId)
    {
        var prog = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prog == null)
            throw new GameException("User does not have a progression", "NO_PROGRESSION", 404);

        prog.AddClick();

        await _context.SaveChangesAsync();
        return new ClickResponse(prog.Count, prog.Multiplier);
    }

    public async Task<int> GetResetCostAsync(int userId)
    {
        var prog = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prog == null)
            throw new GameException("User has no progression", "NO_PROGRESSION", 400);

        return prog.CalculateResetCost();
    }

    public async Task<Progression> ResetProgressionAsync(int userId)
    {
        _logger.LogInformation("Reset attempt: UserId {UserId}", userId);

        var prog = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prog == null)
            throw new GameException("User does not have a progression", "NO_PROGRESSION", 400);

        int cost = prog.CalculateResetCost();
        if (prog.Count < cost)
            throw new GameException("Not enough clicks to reset", "INSUFFICIENT_CLICKS", 400);

        if (prog.Count > GlobaleScore.BestScore)
        {
            GlobaleScore.BestScore = prog.Count;
            GlobaleScore.UserId = userId;
        }
        if (prog.Count > prog.BestScore)
            prog.BestScore = prog.Count;
        var user = await _context.Users.FindAsync(userId);
        var userName = user?.Username ?? $"User#{userId}";
        var scoreBeforeReset = prog.Count;
        prog.Count = 0;
        prog.Multiplier++;

        await _context.SaveChangesAsync();
        await _hubContext.Clients.All.SendAsync(
            "ReceiveMessage",
            "SYSTEM",
            $"{userName} a reset son score de {scoreBeforeReset} points !"
        );


        _logger.LogInformation("Reset success: UserId {UserId} NewMultiplier {Multiplier}", userId, prog.Multiplier);

        return prog;
    }

    public async Task<BestScoreResponse> GetBestScoreAsync()
    {
        
        var best = await _context.Progressions
            .OrderByDescending(p => p.BestScore)
            .FirstOrDefaultAsync();

        if (best == null)
            throw new GameException("No progressions found", "NO_PROGRESSIONS", 404);

        return new BestScoreResponse(best.UserId, best.BestScore);
    }
}
