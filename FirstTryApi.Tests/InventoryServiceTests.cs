using FirstTryApi.Models;
using FirstTryApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Net.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FirstTryApi.Tests;

public class InventoryServiceTests
{
    [Fact]
    public async Task BuyItemAsync_ShouldDebitMoney_AndAddInventory()
    {
        // ===== ARRANGE =====
        var options = new DbContextOptionsBuilder<UserContext>()
        .UseInMemoryDatabase(databaseName: "TestDb_BuyItem")
        .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
        .Options;


        var context = new UserContext(options);

        var user = new User
        {
            Username = "TestUser",
            Role = UserRole.User,
            Password = "hashed" 
        };
        context.Users.Add(user);
        await context.SaveChangesAsync(); 

        var prog = new Progression(user.Id);
        prog.Count = 100;
        prog.TotalClickValue = 0;
        context.Progressions.Add(prog);

        var item = new Item
        {
            Id = 1,
            Name = "Item1",
            Price = 69,
            MaxQuantity = 99,
            ClickValue = 10
        };
        context.Items.Add(item);

        await context.SaveChangesAsync();

        var httpFactoryMock = new Mock<IHttpClientFactory>();
        httpFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        var loggerMock = new Mock<ILogger<InventoryService>>();

        var inventoryService = new InventoryService(context, httpFactoryMock.Object, loggerMock.Object);

        var invList = await inventoryService.BuyItemAsync(user.Id, item.Id);


        var progDb = await context.Progressions.FirstAsync(p => p.UserId == user.Id);
        Assert.Equal(31, progDb.Count); 

        Assert.Equal(10, progDb.TotalClickValue);

        var invEntry = await context.Inventories.FirstOrDefaultAsync(i => i.UserId == user.Id && i.ItemId == item.Id);
        Assert.NotNull(invEntry);
        Assert.Equal(1, invEntry!.Quantity);

        Assert.NotEmpty(invList);
    }
}
