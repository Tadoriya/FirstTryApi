using System.Linq;
using FirstTryApi.Services;
using Xunit;

namespace FirstTryApi.Tests;

public class ConnectionTrackerServiceTests
{
    [Fact]
    public void AddConnection_ShouldMarkUserOnline_AndTrackConnection()
    {
        var tracker = new ConnectionTrackerService();

        tracker.AddConnection(10, "c1");

        Assert.True(tracker.IsOnline(10));
        Assert.Equal(1, tracker.OnlineUserCount);

        var conns = tracker.GetConnections(10).ToList();
        Assert.Single(conns);
        Assert.Equal("c1", conns[0]);

        Assert.True(tracker.TryGetUserId("c1", out var userId));
        Assert.Equal(10, userId);
    }

    [Fact]
    public void AddConnection_TwiceSameUser_ShouldNotIncreaseOnlineCount()
    {
        var tracker = new ConnectionTrackerService();

        tracker.AddConnection(10, "c1");
        tracker.AddConnection(10, "c2");

        Assert.True(tracker.IsOnline(10));
        Assert.Equal(1, tracker.OnlineUserCount);

        var conns = tracker.GetConnections(10).OrderBy(x => x).ToList();
        Assert.Equal(2, conns.Count);
        Assert.Contains("c1", conns);
        Assert.Contains("c2", conns);
    }

    [Fact]
    public void RemoveConnection_ShouldRemoveOnlyThatConnection()
    {
        var tracker = new ConnectionTrackerService();

        tracker.AddConnection(10, "c1");
        tracker.AddConnection(10, "c2");

        tracker.RemoveConnection("c1");

        Assert.True(tracker.IsOnline(10));
        Assert.Equal(1, tracker.OnlineUserCount);

        var conns = tracker.GetConnections(10).ToList();
        Assert.Single(conns);
        Assert.Equal("c2", conns[0]);

        Assert.False(tracker.TryGetUserId("c1", out _));
        Assert.True(tracker.TryGetUserId("c2", out var userId));
        Assert.Equal(10, userId);
    }

    [Fact]
    public void RemoveConnection_LastConnection_ShouldMakeUserOffline()
    {
        var tracker = new ConnectionTrackerService();

        tracker.AddConnection(10, "c1");
        Assert.Equal(1, tracker.OnlineUserCount);

        tracker.RemoveConnection("c1");

        Assert.False(tracker.IsOnline(10));
        Assert.Equal(0, tracker.OnlineUserCount);
        Assert.Empty(tracker.GetConnections(10));
    }
}
