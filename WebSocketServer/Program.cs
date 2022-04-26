using System.Net.WebSockets;
using WebSocketServer.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebSocketManager();

var app = builder.Build();
app.UseWebSockets();
app.UseWebSocketServer();
// app.MapGet("/", () => "Hello World!");

app.Run(async context =>
{
    System.Console.WriteLine("Hello from the 3rd request delegate.");
    await context.Response.WriteAsync("Hello from the 3rd request delegate.");
});

app.Run();

