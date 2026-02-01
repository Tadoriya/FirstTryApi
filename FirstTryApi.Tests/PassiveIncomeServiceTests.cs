using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FirstTryApi.Services;
using FirstTryApi.Models;
using FirstTryApi.Hubs;

namespace FirstTryApi.Tests
{
    public class PassiveIncomeServiceTests
    {
        // ================= HELPERS =================

        private IServiceProvider BuildProvider(string dbName)
        {
            var services = new ServiceCollection();

            services.AddDbContext<UserContext>(opt =>
                    opt.UseInMemoryDatabase(dbName),
                ServiceLifetime.Scoped);

            services.AddSingleton<ConnectionTrackerService>();

            return services.BuildServiceProvider();
        }

        private PassiveIncomeService CreateService(
            IServiceProvider provider,
            ConnectionTrackerService tracker,
            out Mock<IHubContext<ChatHub>> hubContextMock,
            out Mock<IHubClients> hubClientsMock,
            out Mock<ISingleClientProxy> singleClientProxyMock)
        {
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            var logger = Mock.Of<ILogger<PassiveIncomeService>>();

            hubContextMock = new Mock<IHubContext<ChatHub>>();
            hubClientsMock = new Mock<IHubClients>();
            singleClientProxyMock = new Mock<ISingleClientProxy>();

            hubContextMock.SetupGet(h => h.Clients).Returns(hubClientsMock.Object);

            hubClientsMock
                .Setup(c => c.Client(It.IsAny<string>()))
                .Returns(singleClientProxyMock.Object);

            return new PassiveIncomeService(scopeFactory, logger, hubContextMock.Object, tracker);
        }

        private async Task RunServiceOneTickAsync(PassiveIncomeService service, int ms = 150)
        {
            using var cts = new CancellationTokenSource();

            await service.StartAsync(cts.Token);

            await Task.Delay(ms);

            cts.Cancel();
            await service.StopAsync(CancellationToken.None);
        }

        private async Task SeedProgressionAsync(IServiceProvider provider, int userId, int count)
        {
            using var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<UserContext>();

            ctx.Progressions.Add(new Progression(userId) { Count = count });
            await ctx.SaveChangesAsync();
        }

        private async Task<int> ReadCountAsync(IServiceProvider provider, int userId)
        {
            using var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<UserContext>();

            var p = await ctx.Progressions.SingleAsync(p => p.UserId == userId);
            return p.Count;
        }


        [Fact]
        public async Task ExecuteAsync_ShouldIncrementAllProgressions()
        {
            var provider = BuildProvider(nameof(ExecuteAsync_ShouldIncrementAllProgressions));

            await SeedProgressionAsync(provider, 1, 5);
            await SeedProgressionAsync(provider, 2, 10);

            var tracker = new ConnectionTrackerService();
            var service = CreateService(provider, tracker, out _, out _, out _);

            await RunServiceOneTickAsync(service);

            Assert.Equal(6, await ReadCountAsync(provider, 1));
            Assert.Equal(11, await ReadCountAsync(provider, 2));
        }


        [Fact]
        public async Task ExecuteAsync_ShouldClampToIntMaxValue()
        {
            var provider = BuildProvider(nameof(ExecuteAsync_ShouldClampToIntMaxValue));

            await SeedProgressionAsync(provider, 1, int.MaxValue);

            var tracker = new ConnectionTrackerService();
            var service = CreateService(provider, tracker, out _, out _, out _);

            await RunServiceOneTickAsync(service);

            Assert.Equal(int.MaxValue, await ReadCountAsync(provider, 1));
        }


        [Fact]
        public async Task ExecuteAsync_ShouldSendScoreUpdate_OnlyToOnlineUsers()
        {
            var provider = BuildProvider(nameof(ExecuteAsync_ShouldSendScoreUpdate_OnlyToOnlineUsers));

            await SeedProgressionAsync(provider, 1, 0);
            await SeedProgressionAsync(provider, 2, 0);

            var tracker = new ConnectionTrackerService();
            tracker.AddConnection(1, "conn-1"); 

            var service = CreateService(provider, tracker,
                out _, out var hubClientsMock, out var singleClientProxyMock);

            hubClientsMock.Setup(c => c.Client("conn-1")).Returns(singleClientProxyMock.Object);

            await RunServiceOneTickAsync(service);

            singleClientProxyMock.Verify(p =>
                p.SendCoreAsync(
                    "ScoreUpdate",
                    It.Is<object[]>(args => args.Length >= 1 && args[0] is int),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);

            hubClientsMock.Verify(c => c.Client(It.Is<string>(id => id != "conn-1")), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotFail_WhenNoOnlineUsers()
        {
            var provider = BuildProvider(nameof(ExecuteAsync_ShouldNotFail_WhenNoOnlineUsers));

            await SeedProgressionAsync(provider, 1, 1);

            var tracker = new ConnectionTrackerService(); 
            var service = CreateService(provider, tracker, out _, out _, out _);

            var ex = await Record.ExceptionAsync(async () => await RunServiceOneTickAsync(service));
            Assert.Null(ex);
        }
    }
}
