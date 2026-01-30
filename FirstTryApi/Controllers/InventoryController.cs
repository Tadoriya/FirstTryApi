using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FirstTryApi.Exceptions;
using FirstTryApi.Models;
using FirstTryApi.Services;

namespace FirstTryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;

    public InventoryController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            throw new GameException("Invalid token", "INVALID_TOKEN", 401);

        return userId;
    }

    [HttpGet("Seed")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> Seed()
        => Ok(await _inventoryService.SeedInventoryAsync());

    [HttpGet("Items")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Item>>> GetItems()
        => Ok(await _inventoryService.GetAllItemsAsync());

    [HttpGet("UserInventory")]
    public async Task<ActionResult<IEnumerable<InventoryEntry>>> GetUserInventory()
        => Ok(await _inventoryService.GetUserInventoryAsync(GetUserId()));

    [HttpPost("Buy/{itemId}")]
    public async Task<ActionResult<IEnumerable<InventoryEntry>>> BuyItem(int itemId)
        => Ok(await _inventoryService.BuyItemAsync(GetUserId(), itemId));
}
