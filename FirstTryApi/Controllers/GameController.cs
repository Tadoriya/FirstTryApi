using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FirstTryApi.Services;
using FirstTryApi.Models;
using FirstTryApi.Exceptions;
using Microsoft.AspNetCore.RateLimiting;

namespace FirstTryApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class GameController : ControllerBase
{
    private readonly GameService _gameService;

    public GameController(GameService gameService)
    {
        _gameService = gameService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            throw new GameException("Invalid token", "INVALID_TOKEN", 401);

        return userId;
    }

    [HttpGet("Progression")]
    public async Task<ActionResult<Progression>> GetProgression()
        => Ok(await _gameService.GetProgressionAsync(GetUserId()));

    [HttpGet("Initialize")]
    public async Task<ActionResult<Progression>> InitProgression()
        => Ok(await _gameService.InitializeProgressionAsync(GetUserId()));

    [HttpGet("Click")]
    [EnableRateLimiting("perUser")]
    public async Task<ActionResult<ClickResponse>> Click()
        => Ok(await _gameService.ClickAsync(GetUserId()));

    [HttpGet("ResetCost")]
    public async Task<ActionResult<int>> GetResetCost()
        => Ok(await _gameService.GetResetCostAsync(GetUserId()));

    [HttpPost("Reset")]
    public async Task<ActionResult<Progression>> Reset()
        => Ok(await _gameService.ResetProgressionAsync(GetUserId()));

    [HttpGet("BestScore")]
    public async Task<ActionResult<BestScoreResponse>> GetBestScore()
        => Ok(await _gameService.GetBestScoreAsync());
}
