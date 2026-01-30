using FirstTryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FirstTryApi.Services;

public class PassiveIncomeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PassiveIncomeService> _logger;

    public PassiveIncomeService(IServiceScopeFactory scopeFactory, ILogger<PassiveIncomeService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
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
                    p.Count += 1; // +1 point
                }

                await context.SaveChangesAsync(stoppingToken);

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
