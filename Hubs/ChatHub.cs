using Microsoft.AspNetCore.SignalR;

namespace ChatPortal.Hubs;

public class ChatHub : Hub
{
    public async Task SendMessage(string sessionId, string role, string content)
    {
        await Clients.Group(sessionId).SendAsync("ReceiveMessage", role, content);
    }

    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }
}
