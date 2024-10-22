using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.SignalR;
using Hub = Microsoft.AspNetCore.SignalR.Hub;


namespace Server.Services;

[Authorize]
public class ChatHub : Hub
{
    private static ConcurrentDictionary<string, string> _onlineUsers = new ConcurrentDictionary<string, string>();

    public override async Task OnConnectedAsync()
    {
        var username = Context.User?.Claims.FirstOrDefault(c => c.Type == "Username")?.Value ?? "Anonymous";
        _onlineUsers[Context.ConnectionId] = username;
        var onlineUsers = JsonSerializer.Serialize(_onlineUsers.ToList());
        await Clients.All.SendAsync("UpdateOnlineUsers", onlineUsers);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _onlineUsers.TryRemove(Context.ConnectionId, out _ );
        var onlineUsers = JsonSerializer.Serialize(_onlineUsers.ToList());
        await Clients.All.SendAsync("UpdateOnlineUsers", onlineUsers);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string method, string jsonMessage)
    {
        await Clients.All.SendAsync("ReceiveMessage", jsonMessage);
    }
    
    
}