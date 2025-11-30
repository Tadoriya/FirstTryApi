using Microsoft.EntityFrameworkCore;
namespace FirstTryApi.Models;

public class UserContext : DbContext
{
    public UserContext(DbContextOptions<UserContext> options) : base(options)
    {         
    }

    public DbSet<User> Users { get; set; } = null! ;
    public DbSet<Progression> Progressions { get; set; } = null!;
    public DbSet<Item> Items { get; set; } = null!;
    public DbSet<InventoryEntry> Inventories { get; set; } = null!;
}