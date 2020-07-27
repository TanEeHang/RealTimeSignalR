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
        public string Name { get; set; }
        public float Price { get; set; }
        public string Url { get; set; }
        public bool AllRooms { get; set; } = true;
    }

    //===========================================================================
    //  Hub
    //===========================================================================
        
    public class PriceHub : Hub
    {
        private static List<Room> rooms = new List<Room>(){ };

        public string Create(string name, string price, string url,string seller)
        {
            var room = new Room();
            room.sellername=seller;
            room.Name = name;
            room.Price = float.Parse(price);
            room.Url = url;
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

        public async Task updateLastPrice(double message)
        {
            await Clients.Caller.SendAsync("receiveLastPrice", message, "caller");
            await Clients.Others.SendAsync("receiveLastPrice", message, "others");
        }

        private async Task UpdateList(string id = null)
        {
            var list = rooms.FindAll(r => r.AllRooms); 

            if (id == null)
            { await Clients.All.SendAsync("UpdateList", list); }
            else
            { await Clients.Caller.SendAsync("UpdateList", list); }
        }

        public async Task DisplayBid()
        {
            var roomId = Context.GetHttpContext().Request.Query["roomId"];

            if(roomId == ""){
                await Clients.Caller.SendAsync("Reject");
                return;
            }

            Room room = rooms.Find(e => e.Id == roomId);
            await Clients.Caller.SendAsync("ReceiveBid", room);
        }

        public async Task UpdatePrice(string roomId, float price = 0)
        {
            if(roomId == null){
                await Clients.Caller.SendAsync("Reject");
                return;
            }

            Room room = rooms.Find(e => e.Id == roomId);
            string msg = String.Empty;

            if(price > room.Price){
                room.Price = price;
                msg = "Price has updated!";
            }
            
            await Clients.Caller.SendAsync("ReceiveUpdateBid", room.Price, msg);
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

         public override async Task OnDisconnectedAsync(Exception exception) 
        {
            string page = Context.GetHttpContext().Request.Query["page"];

            switch (page)
            {
                case "buyer": BuyerDisconnected(); break;
                case "auction": await AuctionDisconnected(); break;
            }

            await base.OnDisconnectedAsync(exception);
        }

        private void BuyerDisconnected()
        {
            // Nothing
        }

        private async Task AuctionDisconnected()
        {
            string id     = Context.ConnectionId;
            string roomId = Context.GetHttpContext().Request.Query["roomId"];

            Room room = rooms.Find(r => r.Id == roomId);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Reject");
                return;
            }

            //Check room is empty
            
                rooms.Remove(room);
                await UpdateList();
        }

    }
}
