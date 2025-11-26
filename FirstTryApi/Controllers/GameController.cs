using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using FirstTryApi.Models;
using System.Threading.Tasks;
using System.Diagnostics.SymbolStore;

namespace FirstTryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly UserContext _context;

        public UserController(UserContext context)
        {
            _context = context;
        }

        [HttpGet("Progression/{userId}")]
        public async Task<ActionResult<Progression>> GetProgression(int id)
        {
            var prog = await _context.Progressions.FirstOrDefaultAsync(u => u.UserId==id);
            if (prog == null)
                return NotFound(new ErrorReponse("Progression not found", "PROGRESSION_NOT_FOUND"));
            return Ok(prog);

        }

        [HttpGet("Initialize/{userId}")]
        public async Task<ActionResult<Progression>> InitProgression(int id)
        {
            var exists = await _context.Progressions.AnyAsync(u => u.UserId==id);
            if (exists)
                return BadRequest(new ErrorReponse("User has already a progression", "PROGRESSION_EXISTS"));

            var prog=new Progression(id);
            try
            {
                
                _context.Progressions.Add(prog);
                await _context.SaveChangesAsync();
                return Ok(prog);
            }
            catch
            {
                return BadRequest(new ErrorReponse("Failed to initialize progression", "INITIALIZATION_FAILED"));
            }

        }

        [HttpPost("Click/{userId}}")]
        public async Task<ActionResult<ClickResponse>> Click(int id)
        {
            var prog = await _context.Progressions.FirstOrDefaultAsync(u => u.UserId == id);
            if (prog == null)
                return NotFound(new ErrorReponse("User does not have a progression", "NO_PROGRESSION"));
            prog.AddClick();
            await _context.SaveChangesAsync();
            return Ok(new ClickResponse(prog.Count,prog.Multiplier));

        }

        [HttpGet("ResetCost/{userId}")]
        public async Task<ActionResult<RestCostResponse>> GetResetCost(int id)
        {
            var prog = await _context.Progressions.FirstOrDefaultAsync(u => u.UserId == id);
            if (prog==null)
                return BadRequest(new ErrorReponse("User has no progression", "NO_PROGRESSION"));

            int cost = prog.CalculateResetCost();
            return Ok(new ResetCostResponse(cost));

        }

        [HttpPost("Reset/{userId}")]
        public async Task<ActionResult<Progressions>> Reset(int id)
        {
            var prog = await _context.Progressions.FirstOrDefaultAsync(u => u.UserId == id);
            if (prog == null)
                return BadRequest(new ErrorReponse("User has no progression", "NO_PROGRESSION"));

            int recost= prog.CalculateResetCost();
            if(prog.Count < recost)
                return BadRequest(new ErrorReponse("Not enough clicks to reset", "INSUFFICIENT_CLICKS"));
            if(prog.Count > GlobaleScore.BestScore)
            {
                GlobaleScore.BestScore = recost;
                GlobalScore.UserId = id;
            }
            prog.Count = 0;
            prog.Multiplier++;
            await _context.SaveChangesAsync();
            return Ok(prog);

        }

        [HttpGet("BestScore")]
        public async Task<ActionResult<BestScoreResponse>> GetBestScore()
        {
            var best = await _context.Progressions.OrderByDescending(u => u.BestScore).FirstOrDefaultAsync();
            if (best == null)
                return NotFound(new ErrorReponse("No progressions found", "NO_PROGRESSIONS"));

            return Ok(new BestScoreResponse(best.UserId, best.BestScore));

        }


    }
}
