using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FirstTryApi.Services;
using FirstTryApi.Models;
using FirstTryApi.Exceptions;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using FirstTryApi.Hubs;
using Microsoft.EntityFrameworkCore;
namespace FirstTryApi.Controllers;

// Controller responsible for all game-related actions
// Exposes endpoints for progression, clicks, reset and best score

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class GameController : ControllerBase
{
    private readonly GameService _gameService;
    private readonly UserContext _context;
    private readonly IHubContext<ChatHub> _hubContext;


    public GameController(GameService gameService,UserContext context,IHubContext<ChatHub> hubContext)
    {
        _gameService = gameService;
        _context = context;
        _hubContext = hubContext;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            throw new GameException("Invalid token", "INVALID_TOKEN", 401);

        return userId;
    }

    // Retrieves the current authenticated user's progression
    [HttpGet("Progression")]
    [Authorize]
    public async Task<ActionResult<Progression>> GetProgression()
        => Ok(await _gameService.GetProgressionAsync(GetUserId()));

    // Initializes a new progression for the current user
    [HttpGet("Initialize")]
    [Authorize]
    public async Task<ActionResult<Progression>> InitProgression()
        => Ok(await _gameService.InitializeProgressionAsync(GetUserId()));


    // Handles a click action and updates the player's score
    [HttpGet("Click")]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public async Task<ClickResponse> Click()
    {
        var userId = GetUserId();

        var response = await _gameService.ClickAsync(userId);
        return response;
    }

    // Returns the cost required to reset the progression
    [HttpGet("ResetCost")]
    [Authorize]
    public async Task<ResetCostResponse> GetResetCost()
    {
        var userId = GetUserId();

        var cost = await _gameService.GetResetCostAsync(userId);
        return cost;
    }


    // Resets the player's progression and increases the multiplier
    [HttpPost("Reset")]
    [Authorize]
    public async Task<Progression> Reset() 
    {
        int userId = GetUserId();
        var progAfter = await _gameService.ResetProgressionAsync(userId);
        return progAfter;
    }

    // Returns the global best score among all players
    [HttpGet("BestScore")]
    [Authorize]
    public async Task<BestScoreResponse> GetBestScore()
    {
        var best = await _gameService.GetBestScoreAsync();
        return best;
    }
}
