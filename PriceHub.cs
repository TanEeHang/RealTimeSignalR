using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace AssignmentApp
{
     public class Room
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }

    public class PriceHub : Hub
    {
        private static List<Room> rooms = new List<Room>()
        {
            
        };

        public string Create()
        {
            var room = new Room();
            rooms.Add(room);
            return room.Id;
        }
        
        public async Task SendText(string name, string message)
        {
            await Clients.Caller.SendAsync("ReceiveText", name, message, "caller");
            await Clients.Others.SendAsync("ReceiveText", name, message, "others");
        }
    }
}