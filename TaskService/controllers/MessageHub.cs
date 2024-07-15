using Microsoft.AspNetCore.SignalR;

namespace TaskService.Controllers
{
    public class MessageHub : Hub
        {
            public async Task SendMessage(string message)
            {
                await Clients.All.SendAsync("ReceiveMessage", message);
            }
        }
}