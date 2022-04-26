using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace WebSocketServer.Middleware
{
    public class WebSocketServerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketServerConnectionManager _manager;

        public WebSocketServerMiddleware(RequestDelegate next, WebSocketServerConnectionManager manager)
        {
            _next = next;
            _manager = manager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // WriteRequestParam(context);
            if (context.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                System.Console.WriteLine("WebSocket Connected");
                string ConnectionID = _manager.AddSocket(webSocket);
                await SendConnectionIdAsync(webSocket, ConnectionID);
                await RecieveMessage(webSocket, async (result, buffer) =>
                {
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        System.Console.WriteLine("Message recieved");
                        System.Console.WriteLine($"Message: {message}");
                        await RouteJSONMessageAsync(message);
                        return;
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        string id = _manager.GetAllSockets().FirstOrDefault(s => s.Value == webSocket).Key;
                        System.Console.WriteLine("Recieved close message.");
                        _manager.GetAllSockets().TryRemove(id, out WebSocket socket);
                        await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                        return;
                    }
                });
            }
            else
            {
                System.Console.WriteLine("Hellow from the 2nd request delegate.");
                await _next(context);
            }
        }

        private async Task SendConnectionIdAsync(WebSocket socket, string ConnectionId)
        {
            var buffer = Encoding.UTF8.GetBytes("ConnectionId: " + ConnectionId);
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task RecieveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), cancellationToken: CancellationToken.None);
                handleMessage(result, buffer);
            }
        }

        private void WriteRequestParam(HttpContext context)
        {
            System.Console.WriteLine("Request Method: " + context.Request.Method);
            System.Console.WriteLine("Request Protocol: " + context.Request.Protocol);

            if (context.Request.Headers != null)
            {
                foreach (var header in context.Request.Headers)
                {
                    System.Console.WriteLine("--> " + header.Key + " , " + header.Value);
                }
            }
        }

        public async Task RouteJSONMessageAsync(string message)
        {
            var routeObject = JsonConvert.DeserializeObject<dynamic>(message);
            if (Guid.TryParse(routeObject.To.ToString(), out Guid guidOutput))
            {
                System.Console.WriteLine("Targeted");
                var socket = _manager.GetAllSockets().FirstOrDefault(s => s.Key == routeObject.To.ToString());
                if (socket.Value != null)
                {
                    if (socket.Value.State == WebSocketState.Open)
                    {
                        await socket.Value.SendAsync(Encoding.UTF8.GetBytes(routeObject.Message.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                else
                {
                    System.Console.WriteLine("Invalid recipient");
                }
            }
            else
            {
                System.Console.WriteLine("Broadcast");
                foreach (var socket in _manager.GetAllSockets())
                {
                    if (socket.Value.State == WebSocketState.Open)
                    {
                        await socket.Value.SendAsync(Encoding.UTF8.GetBytes(routeObject.Message.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
        }
    }
}
