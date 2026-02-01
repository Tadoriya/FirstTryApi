using Microsoft.AspNetCore.SignalR;
using FirstTryApi.Services;
using System.Threading.Tasks;
using System.Security.Claims;

namespace FirstTryApi.Hubs;

public class ChatHub : Hub
{
    private readonly ConnectionTrackerService _tracker;
    private readonly ILogger<ChatHub> _logger;
    
    public ChatHub(ConnectionTrackerService tracker, ILogger<ChatHub> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }
    public async Task Login(int userId)
    {
        _tracker.AddConnection(userId, Context.ConnectionId);

        await Clients.All.SendAsync("UpdateUserCount", _tracker.OnlineUserCount);
        _logger.LogInformation(
            "ChatHub.Login: userId={userId}, connectionId={connectionId}, online={online}",
            userId, Context.ConnectionId, _tracker.OnlineUserCount
        );
    }

    public override async Task OnConnectedAsync()
    {
        int count = _tracker.OnlineUserCount;
        _logger.LogInformation("ChatHub: Client connected. Total online users: {count}", count);

        await Clients.Caller.SendAsync("UpdateUserCount", count);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _tracker.RemoveConnection(Context.ConnectionId);
        int count = _tracker.OnlineUserCount;
        _logger.LogInformation("ChatHub: Client disconnected. Total online users: {count}", count);

        try
        {
            await Clients.All.SendAsync("UpdateUserCount", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending UpdateUserCount");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string userName, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", userName, message);
    }
}
