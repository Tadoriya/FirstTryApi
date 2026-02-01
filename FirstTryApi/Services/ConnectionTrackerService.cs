using System.Collections.Concurrent;

namespace FirstTryApi.Services;

public class ConnectionTrackerService
{
    private readonly ConcurrentDictionary<int, HashSet<string>> _userConnections = new();

    private readonly ConcurrentDictionary<string, int> _connectionToUser = new();

    public int OnlineUserCount => _userConnections.Count;

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

    public bool IsOnline(int userId) => _userConnections.ContainsKey(userId);

    public bool TryGetUserId(string connectionId, out int userId)
        => _connectionToUser.TryGetValue(connectionId, out userId);
}
