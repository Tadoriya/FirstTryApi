using System.Reflection;
using FirstTryApi.Exceptions;
using FirstTryApi.Hubs;
using FirstTryApi.Models;
using FirstTryApi.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FirstTryApi.Tests;

public class GameServiceTests
{

    private static UserContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<UserContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new UserContext(options);
    }

    private static void ResetGameServiceStatics()
    {
        typeof(GameService)
            .GetField("_cachedHighScore", BindingFlags.NonPublic | BindingFlags.Static)!
            .SetValue(null, 0L);

        typeof(GameService)
            .GetField("_cachedHighScoreUserId", BindingFlags.NonPublic | BindingFlags.Static)!
            .SetValue(null, 0);

        // Reset GlobaleScore (si tu l'utilises aussi dans ClickAsync)
        GlobaleScore.BestScore = 0;
        GlobaleScore.UserId = 0;
    }

    private static (Mock<IHubContext<ChatHub>> hub, Mock<IClientProxy> allClient) CreateHubMocks()
    {
        var hub = new Mock<IHubContext<ChatHub>>();
        var clients = new Mock<IHubClients>();
        var allClient = new Mock<IClientProxy>();

        hub.SetupGet(h => h.Clients).Returns(clients.Object);
        clients.SetupGet(c => c.All).Returns(allClient.Object);

        allClient
            .Setup(c => c.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()
            ))
            .Returns(Task.CompletedTask);

        return (hub, allClient);
    }

    

    private static ILogger<GameService> CreateNullLogger()
        => new Mock<ILogger<GameService>>().Object;


    [Fact]
    public async Task InitializeProgressionAsync_ShouldCreate_WhenNotExists()
    {   
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(InitializeProgressionAsync_ShouldCreate_WhenNotExists));

        var service = new GameService(ctx, CreateNullLogger(), hubContext: null);

        var prog = await service.InitializeProgressionAsync(userId: 1);

        Assert.NotNull(prog);
        Assert.Equal(1, prog.UserId);

        var inDb = await ctx.Progressions.FirstOrDefaultAsync(p => p.UserId == 1);
        Assert.NotNull(inDb);
    }

    [Fact]
    public async Task InitializeProgressionAsync_ShouldThrow_WhenAlreadyExists()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(InitializeProgressionAsync_ShouldThrow_WhenAlreadyExists));

        ctx.Progressions.Add(new Progression(1));
        await ctx.SaveChangesAsync();

        var service = new GameService(ctx, CreateNullLogger(), hubContext: null);

        var ex = await Assert.ThrowsAsync<GameException>(() => service.InitializeProgressionAsync(1));
        Assert.Equal("PROGRESSION_EXISTS", ex.Code);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task GetProgressionAsync_ShouldReturn_WhenExists()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(GetProgressionAsync_ShouldReturn_WhenExists));

        ctx.Progressions.Add(new Progression(2) { Count = 10 });
        await ctx.SaveChangesAsync();

        var service = new GameService(ctx, CreateNullLogger(), hubContext: null);

        var prog = await service.GetProgressionAsync(2);
        Assert.Equal(10, prog.Count);
    }

    [Fact]
    public async Task GetProgressionAsync_ShouldThrow_WhenMissing()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(GetProgressionAsync_ShouldThrow_WhenMissing));

        var service = new GameService(ctx, CreateNullLogger(), hubContext: null);

        var ex = await Assert.ThrowsAsync<GameException>(() => service.GetProgressionAsync(999));
        Assert.Equal("PROGRESSION_NOT_FOUND", ex.Code);
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task ClickAsync_ShouldThrow_WhenNoProgression()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(ClickAsync_ShouldThrow_WhenNoProgression));

        var service = new GameService(ctx, CreateNullLogger(), hubContext: null);

        var ex = await Assert.ThrowsAsync<GameException>(() => service.ClickAsync(1));
        Assert.Equal("NO_PROGRESSION", ex.Code);
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task ClickAsync_ShouldIncreaseCount_AndNotSendHighScore_WhenHubNull()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(ClickAsync_ShouldIncreaseCount_AndNotSendHighScore_WhenHubNull));

        ctx.Users.Add(new User { Id = 1, Username = "Messi" });
        ctx.Progressions.Add(new Progression(1) { Count = 0, Multiplier = 2, TotalClickValue = 3 }); 
        await ctx.SaveChangesAsync();

        var service = new GameService(ctx, CreateNullLogger(), hubContext: null);

        var res = await service.ClickAsync(1);
        Assert.Equal(8, res.Count);
        Assert.Equal(2, res.Multiplier);
    }

    [Fact]
    public async Task ClickAsync_ShouldClampOverflow_ToIntMax()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(ClickAsync_ShouldClampOverflow_ToIntMax));

        ctx.Users.Add(new User { Id = 1, Username = "Messi" });
        ctx.Progressions.Add(new Progression(1)
        {
            Count = int.MaxValue - 1,
            Multiplier = 10,
            TotalClickValue = 10 
        });
        await ctx.SaveChangesAsync();

        var service = new GameService(ctx, CreateNullLogger(), hubContext: null);

        var res = await service.ClickAsync(1);
        Assert.Equal(int.MaxValue, res.Count);
    }

    [Fact]
    public async Task ClickAsync_ShouldNotSendNewHighScore_WhenNotBeaten()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(ClickAsync_ShouldNotSendNewHighScore_WhenNotBeaten));

        ctx.Users.Add(new User { Id = 1, Username = "Neymar" });
        ctx.Progressions.Add(new Progression(1) { Count = 0, Multiplier = 1, TotalClickValue = 0 });
        await ctx.SaveChangesAsync();

        var (hub, allClient) = CreateHubMocks();
        var service = new GameService(ctx, CreateNullLogger(), hub.Object);

        await service.ClickAsync(1);

        var prog = await ctx.Progressions.FirstAsync(p => p.UserId == 1);
        prog.Multiplier = 0;
        prog.TotalClickValue = -1; 
        await ctx.SaveChangesAsync();

        await service.ClickAsync(1);

        allClient.Verify(c => c.SendCoreAsync(
            "NewHighScore",
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetResetCostAsync_ShouldThrow_WhenNoProgression()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(GetResetCostAsync_ShouldThrow_WhenNoProgression));

        var service = new GameService(ctx, CreateNullLogger(), hubContext: null);

        var ex = await Assert.ThrowsAsync<GameException>(() => service.GetResetCostAsync(1));
        Assert.Equal("NO_PROGRESSION", ex.Code);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task ResetProgressionAsync_ShouldThrow_WhenNoProgression()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(ResetProgressionAsync_ShouldThrow_WhenNoProgression));

        var service = new GameService(ctx, CreateNullLogger(), hubContext: null);

        var ex = await Assert.ThrowsAsync<GameException>(() => service.ResetProgressionAsync(1));
        Assert.Equal("NO_PROGRESSION", ex.Code);
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task ResetProgressionAsync_ShouldThrow_WhenInsufficientClicks()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(ResetProgressionAsync_ShouldThrow_WhenInsufficientClicks));

        ctx.Users.Add(new User { Id = 1, Username = "Dybala" });
        ctx.Progressions.Add(new Progression(1) { Count = 0, Multiplier = 1 }); 
        await ctx.SaveChangesAsync();

        var service = new GameService(ctx, CreateNullLogger(), hubContext: null);

        var ex = await Assert.ThrowsAsync<GameException>(() => service.ResetProgressionAsync(1));
        Assert.Equal("INSUFFICIENT_CLICKS", ex.Code);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task ClickAsync_ShouldSendNewHighScore_WhenRecordBeaten_AndHubProvided()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(ClickAsync_ShouldSendNewHighScore_WhenRecordBeaten_AndHubProvided));

        ctx.Users.Add(new User { Id = 1, Username = "Hazard" });
        ctx.Progressions.Add(new Progression(1) { Count = 0, Multiplier = 1, TotalClickValue = 0 });
        await ctx.SaveChangesAsync();

        var (hub, allClient) = CreateHubMocks();
        var service = new GameService(ctx, CreateNullLogger(), hub.Object);

        var res = await service.ClickAsync(1);
        Assert.Equal(1, res.Count);

        allClient.Verify(c => c.SendCoreAsync(
            "NewHighScore",
            It.Is<object[]>(args =>
                (string)args[0] == "Hazard" &&
                (long)args[1] == 1L
            ),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetBestScoreAsync_ShouldThrow_WhenNoProgressionsOrZero()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(GetBestScoreAsync_ShouldThrow_WhenNoProgressionsOrZero));

        var service = new GameService(ctx, CreateNullLogger(), hubContext: null);

        var ex = await Assert.ThrowsAsync<GameException>(() => service.GetBestScoreAsync());
        Assert.Equal("NO_PROGRESSIONS", ex.Code);
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task GetBestScoreAsync_ShouldReturnBest()
    {
        ResetGameServiceStatics();
        using var ctx = CreateInMemoryContext(nameof(GetBestScoreAsync_ShouldReturnBest));

        ctx.Progressions.Add(new Progression(1) { BestScore = 10 });
        ctx.Progressions.Add(new Progression(2) { BestScore = 50 });
        await ctx.SaveChangesAsync();

        var service = new GameService(ctx, CreateNullLogger(), hubContext: null);

        var best = await service.GetBestScoreAsync();
        Assert.Equal(2, best.Userid);
        Assert.Equal(50, best.BestScore);
    }
}
