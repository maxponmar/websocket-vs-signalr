using System;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SignalRServer
{
  public class ChatHub : Hub
  {
    public override Task OnConnectedAsync()
    {
      Console.WriteLine("==> Connection Established." + Context.ConnectionId);
      Clients.Client(Context.ConnectionId).SendAsync("ReceiveConnID", Context.ConnectionId);
      return base.OnConnectedAsync();
    }

    public async Task SendMessageAsync(string message)
    {
      var routeOb = JsonConvert.DeserializeObject<dynamic>(message);
      string toClient = routeOb.To;
      Console.WriteLine("Message Recieved on: " + Context.ConnectionId);
      if (toClient == string.Empty)
      {
        // Broadcast message 
        await Clients.All.SendAsync("RecieveMessage", message);
      }
      else
      {
        await Clients.Client(toClient).SendAsync("RecieveMessage", message);
      }
    }
  }
}