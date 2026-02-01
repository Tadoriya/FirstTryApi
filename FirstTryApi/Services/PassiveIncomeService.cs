using FirstTryApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using FirstTryApi.Hubs;

namespace FirstTryApi.Services;

public class PassiveIncomeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PassiveIncomeService> _logger;
    private readonly IHubContext<ChatHub> _hubContext;               
    private readonly ConnectionTrackerService _tracker; 

    public PassiveIncomeService(IServiceScopeFactory scopeFactory, ILogger<PassiveIncomeService> logger,IHubContext<ChatHub> hubContext, ConnectionTrackerService tracker)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hubContext = hubContext;                                  
        _tracker = tracker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<UserContext>();

                    var progressions = await context.Progressions.ToListAsync(stoppingToken);

                    foreach (var p in progressions)
                    {
                        if (p.Count < int.MaxValue) 
                            p.Count += 1;
                        else
                            p.Count = int.MaxValue;
                    }

                    await context.SaveChangesAsync(stoppingToken);

                    foreach (var p in progressions)
                    {
                        int userId = p.UserId;

                        if (!_tracker.IsOnline(userId))
                            continue;

                        var connections = _tracker.GetConnections(userId);
                        foreach (var connectionId in connections)
                        {
                            await _hubContext.Clients.Client(connectionId)
                                .SendAsync("ScoreUpdate", p.Count, cancellationToken: stoppingToken);
                        }
                    }

                    _logger.LogInformation("Passive income: +1 given to {Count} users", progressions.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PassiveIncomeService failed");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
}
