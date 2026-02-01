using System.Collections.Concurrent;

namespace FirstTryApi.Services;

// Service used to track connected users and their SignalR connections

public class ConnectionTrackerService
{
    private readonly ConcurrentDictionary<int, HashSet<string>> _userConnections = new();

    private readonly ConcurrentDictionary<string, int> _connectionToUser = new();

    public int OnlineUserCount => _userConnections.Count;

    // Registers a new user connection
    public void AddConnection(int userId, string connectionId)
    {
        _userConnections.AddOrUpdate(
            userId,
            _ => new HashSet<string> { connectionId },
            (_, set) =>
            {
                lock (set)
                {
                    set.Add(connectionId);
                    return set;
                }
            });

        _connectionToUser[connectionId] = userId;
    }

    // Removes a disconnected user
    public void RemoveConnection(string connectionId)
    {
        if (!_connectionToUser.TryRemove(connectionId, out int userId))
            return;

        if (_userConnections.TryGetValue(userId, out var set))
        {
            lock (set)
            {
                set.Remove(connectionId);
                if (set.Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);
                }
            }
        }
    }

    public IEnumerable<string> GetConnections(int userId)
    {
        if (_userConnections.TryGetValue(userId, out var set))
        {
            lock (set)
            {
                return set.ToList();
            }
        }

        return Enumerable.Empty<string>();
    }

    // Checks whether a user is currently online
    public bool IsOnline(int userId) => _userConnections.ContainsKey(userId);

    public bool TryGetUserId(string connectionId, out int userId)
        => _connectionToUser.TryGetValue(connectionId, out userId);
}
