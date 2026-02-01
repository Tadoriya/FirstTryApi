using FirstTryApi.Exceptions;
using FirstTryApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace FirstTryApi.Services;

public class InventoryService
{
    private readonly UserContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InventoryService> _logger;

    private const string Lien = "https://csharp.nouvet.fr/front10/items.json";

    public InventoryService(UserContext context, IHttpClientFactory httpClientFactory, ILogger<InventoryService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SeedInventoryAsync()
    {
        _logger.LogInformation("Inventory seeding started");

        var inventories = await _context.Inventories.ToListAsync();
        var items = await _context.Items.ToListAsync();

        if (inventories.Any())
            _context.Inventories.RemoveRange(inventories);

        if (items.Any())
            _context.Items.RemoveRange(items);

        await _context.SaveChangesAsync();

        var client = _httpClientFactory.CreateClient();
        var downloadedItems = await client.GetFromJsonAsync<List<Item>>(Lien);

        if (downloadedItems == null || downloadedItems.Count == 0)
            throw new GameException("Failed to seed inventory", "SEED_FAILED", 400);

        _context.Items.AddRange(downloadedItems);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Inventory seeding completed: {Count} items added", downloadedItems.Count);

        return true;
    }

    public async Task<List<Item>> GetAllItemsAsync()
    {
        var items = await _context.Items.ToListAsync();
        if (items.Count == 0)
            throw new GameException("No items found", "NO_ITEMS", 404);

        return items;
    }

    public async Task<List<InventoryEntry>> GetUserInventoryAsync(int userId)
    {
        return await _context.Inventories
            .Where(i => i.UserId == userId)
            .ToListAsync();
    }

    public async Task<string> GetUsernameAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.Username ?? "Unknown";
    }

    public async Task<Item?> GetItemByIdAsync(int itemId)
    {
        return await _context.Items.FindAsync(itemId);
    }

    public async Task<List<InventoryEntry>> BuyItemAsync(int userId, int itemId)
    {
        _logger.LogInformation("Item purchase attempt: UserId {UserId}, ItemId {ItemId}", userId, itemId);

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new GameException("User not found", "USER_NOT_FOUND", 404);

        var item = await _context.Items.FindAsync(itemId);
        if (item == null)
            throw new GameException("Item not found", "ITEM_NOT_FOUND", 404);

        var prog = await _context.Progressions.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prog == null)
            throw new GameException("Progression not found", "PROGRESSION_NOT_FOUND", 404);

        if (prog.Count < item.Price)
        {
            _logger.LogWarning(
                "Item purchase failed: Not enough money - UserId {UserId}, ItemId {ItemId}, Available: {Available}, Required: {Required}",
                userId, itemId, prog.Count, item.Price
            );
            throw new GameException("Not enough money", "NOT_ENOUGH_MONEY", 400);
        }

        var inv = await _context.Inventories
            .FirstOrDefaultAsync(i => i.UserId == userId && i.ItemId == itemId);

        if (inv != null)
        {
            if (inv.Quantity >= item.MaxQuantity)
            {
                _logger.LogWarning("Item purchase failed: MaxQuantity reached - UserId {UserId}, ItemId {ItemId}", userId, itemId);
                throw new GameException("Inventory full", "INVENTORY_FULL", 400);
            }

            inv.Quantity++;
        }
        else
        {
            inv = new InventoryEntry
            {
                UserId = userId,
                ItemId = itemId,
                Quantity = 1
            };
            _context.Inventories.Add(inv);
        }

        prog.Count -= item.Price;
        prog.TotalClickValue += item.ClickValue;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Item purchased successfully: UserId {UserId}, ItemId {ItemId}", userId, itemId);

        return await _context.Inventories
            .Where(i => i.UserId == userId)
            .ToListAsync();
    }
}
