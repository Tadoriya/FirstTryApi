using System.Collections.Generic;
using System.Threading.Tasks;
using FirstTryApi.Models;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;


namespace FirstTryApi.Contollers; 


[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly UserContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private const string lien = "https://csharp.nouvet.fr/front4/items.json";

    public InventoryController(UserContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("Seed")]
    public async Task<ActionResult<bool>> Seed()
    {
        try
        {
            var inventories = await _context.Inventories.ToListAsync();
            var items = await _context.Items.ToListAsync();

            if (inventories.Any())
                _context.Inventories.RemoveRange(inventories);

            if (items.Any())
                _context.Items.RemoveRange(items);

            await _context.SaveChangesAsync();
        
            var client = _httpClientFactory.CreateClient();
            var downloadedItems = await client.GetFromJsonAsync<List<Item>>(lien);

            if (downloadedItems == null || downloadedItems.Count == 0)
                return BadRequest(new ErrorResponse("Failed to seed inventory", "SEED_FAILED"));
       
            _context.Items.AddRange(downloadedItems);
            await _context.SaveChangesAsync();

            return Ok(true);
        }
        catch
        {
            return BadRequest(new ErrorResponse("Failed to seed inventory", "SEED_FAILED"));
        }
    }

    [HttpGet("Items")]
    public async Task<ActionResult<IEnumerable<Item>>> GetItems()
    {
        var items = await _context.Items.ToListAsync();
        if(!items.Any())
            return NotFound(new ErrorResponse("No items found","NO_ITEMS"));
        return Ok(items);
    }


    [HttpGet("UserInventory/{userId}")]
    public async Task<ActionResult<IEnumerable<InventoryEntry>>> GetUserInventory(int userId)
    {
        var inv = await _context.Inventories.Where(u => u.UserId == userId).ToListAsync();
        return Ok(inv);
    }

    [HttpPost("Buy/{userId}/{itemId}")]
    public async Task<ActionResult<IEnumerable<InventoryEntry>>> BuyItem(int userId, int itemId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return BadRequest(new ErrorResponse("User not found", "USER_NOT_FOUND"));

        var item = await _context.Items.FindAsync(itemId);
        if (item == null)
            return BadRequest(new ErrorResponse("Item not found", "ITEM_NOT_FOUND"));

        var prog = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);

        if (prog.Count < item.Price)
            return BadRequest(new ErrorResponse("Not enough money", "NOT_ENOUGH_MONEY"));

        prog.Count -= item.Price;

        var inv = await _context.Inventories
            .FirstOrDefaultAsync(u => u.UserId == userId && u.ItemId == itemId);

        if (inv == null)
        {
            inv = new InventoryEntry
            {
                UserId = userId,
                ItemId = itemId,
                Quantity = 1
            };
            _context.Inventories.Add(inv);
        }
        else
        {
            inv.Quantity++;
        }

        prog.TotalClickValue += item.ClickValue;

        await _context.SaveChangesAsync();

        var finalInv = await _context.Inventories
            .Where(u => u.UserId == userId)
            .ToListAsync();

        return Ok(finalInv);
    }


}