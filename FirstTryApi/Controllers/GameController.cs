using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FirstTryApi.Models;
using FirstTryApi.Services;
using System.Threading.Tasks;
using System.Diagnostics.SymbolStore;
using System.Security.Claims;


namespace FirstTryApi.Controllers;


[Authorize]
[Route("api/[controller]")]
[ApiController]
public class GameController : ControllerBase
{
    private readonly UserContext _context;

    public GameController(UserContext context)
    {
        _context = context;
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


    [HttpGet("Progression")]
    [Authorize]
    public async Task<ActionResult<Progression>> GetProgression()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new ErrorResponse("Invalid token", "INVALID_TOKEN"));
        var prog = await _context.Progressions.FirstOrDefaultAsync(u => u.UserId==userId.Value);
        if (prog == null)
            return NotFound(new ErrorResponse("Progression not found", "PROGRESSION_NOT_FOUND"));
        return Ok(prog);

    }

    [HttpGet("Initialize")]
    [Authorize]
    public async Task<ActionResult<Progression>> InitProgression()
    {
        var userId = GetUserId();
        if(userId == null)
            return Unauthorized(new ErrorResponse("Invalid token", "INVALID_TOKEN"));
        var exists = await _context.Progressions.AnyAsync(u => u.UserId==userId.Value);
        if (exists)
            return BadRequest(new ErrorResponse("User has already a progression", "PROGRESSION_EXISTS"));

        var prog=new Progression(userId.Value);
        try
        {
            
            _context.Progressions.Add(prog);
            await _context.SaveChangesAsync();
            return Ok(prog);
        }
        catch
        {
            return BadRequest(new ErrorResponse("Failed to initialize progression", "INITIALIZATION_FAILED"));
        }

    }

    [HttpGet("Click")]
    [Authorize]
    public async Task<ActionResult<ClickResponse>> Click()
    {
        var userId = GetUserId();
        if(userId == null) 
            return Unauthorized(new ErrorResponse("Invalid token", "INVALID_TOKEN"));
        var prog = await _context.Progressions.FirstOrDefaultAsync(u =>u.UserId == userId.Value);
        if (prog == null)
            return NotFound(new ErrorResponse("User does not have a progression", "NO_PROGRESSION"));
        prog.AddClick();
        await _context.SaveChangesAsync();
        return Ok(new ClickResponse(prog.Count,prog.Multiplier));

    }

    [HttpGet("ResetCost")]
    [Authorize]
    public async Task<ActionResult<ResetCostResponse>> GetResetCost()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new ErrorResponse("Invalid token", "INVALID_TOKEN"));
        var prog = await _context.Progressions.FirstOrDefaultAsync(u => u.UserId == userId.Value);
        if (prog==null)
            return BadRequest(new ErrorResponse("User has no progression", "NO_PROGRESSION"));

        int cost = prog.CalculateResetCost();
        return Ok(new ResetCostResponse(cost));

    }

    [HttpPost("Reset")]
    [Authorize]
    public async Task<ActionResult<Progression>> Reset()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new ErrorResponse("Invalid token", "INVALID_TOKEN"));
        var prog = await _context.Progressions.FirstOrDefaultAsync(u => u.UserId == userId.Value);
        if (prog == null)
            return BadRequest(new ErrorResponse("User does not have a progression", "NO_PROGRESSION"));

        int recost= prog.CalculateResetCost();
        if(prog.Count < recost)
            return BadRequest(new ErrorResponse("Not enough clicks to reset", "INSUFFICIENT_CLICKS"));
        if(prog.Count > GlobaleScore.BestScore)
        {
            GlobaleScore.BestScore = recost;
            GlobaleScore.UserId = userId.Value;
        }
        prog.Count = 0;
        prog.Multiplier++;
        await _context.SaveChangesAsync();
        return Ok(prog);

    }

    [HttpGet("BestScore")]
    [Authorize]
    public async Task<ActionResult<BestScoreResponse>> GetBestScore()
    {
        var best = await _context.Progressions.OrderByDescending(u => u.BestScore).FirstOrDefaultAsync();
        if (best == null)
            return NotFound(new ErrorResponse("No progressions found", "NO_PROGRESSIONS"));

        return Ok(new BestScoreResponse(best.UserId, best.BestScore));

    }


}
