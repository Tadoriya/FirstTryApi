using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FirstTryApi.Exceptions;
using FirstTryApi.Models;
using FirstTryApi.Services;
using Microsoft.AspNetCore.SignalR;
using FirstTryApi.Hubs;
using Microsoft.AspNetCore.RateLimiting;

namespace FirstTryApi.Controllers;

    // Controller responsible for inventory and item management
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryService _inventoryService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(InventoryService inventoryService, IHubContext<ChatHub> hubContext, ILogger<InventoryController> logger)
        {
            _inventoryService = inventoryService;
            _hubContext = hubContext;
            _logger = logger;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                throw new GameException("Invalid token", "INVALID_TOKEN", 401);

            return userId;
        }

        // Seeds the inventory with default items
        [HttpGet("Seed")]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> Seed()
            => Ok(await _inventoryService.SeedInventoryAsync());


        // Returns all available items
        [HttpGet("Items")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems()
            => Ok(await _inventoryService.GetAllItemsAsync());

        // Returns the inventory of the current user
        [HttpGet("UserInventory")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<InventoryEntry>>> GetUserInventory()
            => Ok(await _inventoryService.GetUserInventoryAsync(GetUserId()));

        
        // Allows the user to buy an item
        [HttpPost("Buy/{itemId}")]
        [EnableRateLimiting("perUser")]
        public async Task<ActionResult<IEnumerable<InventoryEntry>>> BuyItem(int itemId) 
        {
            int userId = GetUserId(); 
            string username = await _inventoryService.GetUsernameAsync(userId) ;
            _logger.LogInformation("User {UserId} attempts to buy item {ItemId}", userId, itemId);


            var item = await _inventoryService.GetItemByIdAsync(itemId);
            if (item == null)
                throw new GameException("Item not found", "ITEM_NOT_FOUND", 404);

            var inv = await _inventoryService.BuyItemAsync(userId, itemId);
            _logger.LogInformation("User {UserId} bought item {ItemId} ", userId, itemId);

            if (item.Price > 10000)
            {
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveMessage",
                    "SYSTEM",
                    $"{username} vient d'acquérir {item.Name} !"
                );
            }

            return Ok(inv);
            }
            
    }
