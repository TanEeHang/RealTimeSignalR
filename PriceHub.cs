using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace AssignmentApp
{
    public class PriceHub : Hub
    {
        public async Task SendText(string name, string message)
        {
            await Clients.Caller.SendAsync("ReceiveText", name, message, "caller");
            await Clients.Others.SendAsync("ReceiveText", name, message, "others");
        }
    }
}