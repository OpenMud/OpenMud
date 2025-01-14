namespace OpenMud.Debug.Core;
using Microsoft.AspNetCore.SignalR;
public class DebugHub : Hub
{
    private IDebugRuntimeService clientHandler;
    
    public DebugHub(IDebugRuntimeService clientHandler)
    {
        this.clientHandler = clientHandler;
    }

    public override Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;

        Console.WriteLine("Client connected");
        //clientState.AddClient(Context.ConnectionId);
        //return Task.CompletedTask;
        
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine("Client disconnected");

        if (exception != null)
        {
            Console.Error.WriteLine(exception.ToString());
        }
        //clientState.RemoveClient(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public void Start()
    {
        clientHandler.Request(x => x.Start());
    }
    
    public void Shutdown()
    {
        clientHandler.Request(x => x.Shutdown());
    }
}