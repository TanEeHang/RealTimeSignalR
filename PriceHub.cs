using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace AssignmentApp
{
    //===========================================================================
    //  Classes
    //===========================================================================

    public class User
    {
        public string Id { get; set; }
        public string Role { get; set; }
        public string Name { get; set; }
        public User(string id, string role, string name) => (Id, Role, Name) = (id, role, name);
    }

     public class Room
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public bool AllRooms { get; set; } = true;
    }

    //===========================================================================
    //  Hub
    //===========================================================================
        
    public class PriceHub : Hub
    {
        private static List<Room> rooms = new List<Room>(){   };

        public string Create()
        {
            var room = new Room();
            rooms.Add(room);
            return room.Id;
        }

        //===========================================================================
        //  FUNCTIONS
        //===========================================================================
        
        public async Task SendText(string name, string message)
        {
            await Clients.Caller.SendAsync("ReceiveText", name, message, "caller");
            await Clients.Others.SendAsync("ReceiveText", name, message, "others");
        }

        private async Task UpdateList(string id = null)
        {
            var list = rooms.FindAll(r => r.AllRooms); 

            if (id == null)
            { await Clients.All.SendAsync("UpdateList", list); }
            else
            { await Clients.Caller.SendAsync("UpdateList", list); }
        }

        //===========================================================================
        //  CONNECT AND DISCONNECT
        //===========================================================================

         public override async Task OnConnectedAsync()
        {
            string page = Context.GetHttpContext().Request.Query["page"];

            switch (page)
            {
                case "buyer": await BuyConnected(); break;
                case "auction": await AuctionConnected(); break;
            }

            await base.OnConnectedAsync();
        }

        private async Task BuyConnected()
        {
            await UpdateList();
        }

        private async Task AuctionConnected()
        {
            string id     = Context.ConnectionId;
            string role   = Context.GetHttpContext().Request.Query["role"];
            string name   = Context.GetHttpContext().Request.Query["name"];
            string roomId = Context.GetHttpContext().Request.Query["roomId"];

            Room room = rooms.Find(g => g.Id == roomId);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Reject");
                return;
            }

            User u = new User(id, role, name);
            // TODO: Check role fucntion
            await Groups.AddToGroupAsync(id, roomId);

            await UpdateList();
        }

    }
}