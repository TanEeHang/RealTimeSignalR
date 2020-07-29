using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Timers;
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
        public int countdown{get;set;}
        public string sellername { get; set; }
        public float Price { get; set; }
        public string Url { get; set; }
        public int Count { get; set; } = 0;
        public int PeopleMax { get; set; }
        public bool AllRooms => Count >= PeopleMax;
        
    }

    //===========================================================================
    //  Hub
    //===========================================================================
        
    public class PriceHub : Hub
    {
        private static List<Room> rooms = new List<Room>(){ };

        public string Create(string name, string price, string url,string seller,string timer, string people)
        {
            var room = new Room();
            room.sellername=seller;
            room.countdown=int.Parse(timer);
            room.Name = name;
            room.Price = float.Parse(price);
            room.Url = url;
            //var noConvert = startTime;
            //room.StartTime = DateTime.ParseExact(noConvert, "HH:mm", CultureInfo.InvariantCulture);
            int convertPP = Convert.ToInt32( people );
            room.PeopleMax = convertPP;
            rooms.Add(room);
            return room.Id;
        }

        //===========================================================================
        //  FUNCTIONS
        //===========================================================================
        
        public async Task SendText(string name, string message)
        {
            var roomId = Context.GetHttpContext().Request.Query["roomId"];

            Room room = rooms.Find(e => e.Id == roomId);

            if(room == null){
                await Clients.Caller.SendAsync("Reject");
                return;
            }
            
            await Clients.Group(roomId).SendAsync("ReceiveText", name, message);
        }

        public async Task updateLastPrice(double message)
        {
            var roomId = Context.GetHttpContext().Request.Query["roomId"];

            Room room = rooms.Find(e => e.Id == roomId);

            if(room == null){
                await Clients.Caller.SendAsync("Reject");
                return;
            }

            await Clients.Group(roomId).SendAsync("receiveLastPrice", message);
        }

        public async Task updateLastBidID(string name)
        {
            string id = Context.ConnectionId;
            var roomId = Context.GetHttpContext().Request.Query["roomId"];

            Room room = rooms.Find(e => e.Id == roomId);

            if(room == null){
                await Clients.Caller.SendAsync("Reject");
                return;
            }

            await Clients.Group(roomId).SendAsync("getLastBidID", id, name);
        }

        public async Task updateOwnID()
        {
            string id = Context.ConnectionId;
            await Clients.Caller.SendAsync("getOwnID", id, "caller");
        }

        public async Task Start(bool flag, int timer)
        {
            var roomId = Context.GetHttpContext().Request.Query["roomId"];

            Room room = rooms.Find(e => e.Id == roomId);

            if(room == null){
                await Clients.Caller.SendAsync("Reject");
                return;
            }
            if (timer == 0){
                await Clients.Group(roomId).SendAsync("StartTimer", flag, room.countdown);
            }
            else{
                await Clients.Group(roomId).SendAsync("StartTimer", flag, timer);
            }
        }


        public async Task UpdateList(string id = null)
        {
            var list = rooms.FindAll(r => r.AllRooms == false); 
            if (id == null)
            { 
                 await Clients.All.SendAsync("UpdateList", list); 
            }
            else
            { 
                await Clients.All.SendAsync("UpdateList", list); 
            }
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

        //===========================================================================
        //  CHAT
        //===========================================================================
        public async Task SendChat(string name, string message)
        {
            await Clients.Caller.SendAsync("ReceiveChat", name, message, "caller");
            await Clients.Others.SendAsync("ReceiveChat", name, message, "others");
        }

        //===========================================================================
        //  CONNECT AND DISCONNECT
        //===========================================================================

         public override async Task OnConnectedAsync()
        {
            string page = Context.GetHttpContext().Request.Query["page"];
            string name = Context.GetHttpContext().Request.Query["name"];

            switch (page)
            {
                case "buyer": await BuyConnected(); break;
                case "auction": await AuctionConnected(); break;
            }

            await base.OnConnectedAsync();
            await Clients.All.SendAsync("UpdateChatStatus", $"<b>{name}</b> joined");
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
            await Groups.AddToGroupAsync(id, roomId);
            room.Count++;
            await Clients.Group(roomId).SendAsync("UpdateCount", room.Count, room.PeopleMax);
            await UpdateList();
        }

         public override async Task OnDisconnectedAsync(Exception exception) 
        {
            string page = Context.GetHttpContext().Request.Query["page"];
            string name = Context.GetHttpContext().Request.Query["name"];

            switch (page)
            {
                case "buyer": BuyerDisconnected(); break;
                case "auction": await AuctionDisconnected(); break;
            }

            await base.OnDisconnectedAsync(exception);
            await Clients.All.SendAsync("UpdateChatStatus", $"<b>{name}</b> left");
        }

        private void BuyerDisconnected()
        {
            // Do Nothing
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

            room.Count--;
            await Clients.Group(roomId).SendAsync("UpdateCount", room.Count, room.PeopleMax);

            //Check room is empty
            if(room.Count <= 0){
                rooms.Remove(room);
                await UpdateList();
            }
        }

    }
}
