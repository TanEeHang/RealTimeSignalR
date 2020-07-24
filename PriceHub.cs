using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ChatRT
{
    public class ChatHub : Hub
    {
        private static int count = 0;

        public async Task SendText(string name, string message)
        {
            await Clients.Caller.SendAsync("ReceiveText", name, message, "caller");
            await Clients.Others.SendAsync("ReceiveText", name, message, "others");
        }

        public override async Task OnConnectedAsync()
        {
            count++;
            string name = Context.GetHttpContext().Request.Query["name"];
            await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> joined");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception) 
        {
            count--;
            string name = Context.GetHttpContext().Request.Query["name"];
            await Clients.All.SendAsync("UpdateStatus", count, $"<b>{name}</b> left");
            await base.OnDisconnectedAsync(exception);
        }       
    }
}