using Cysharp.Threading;
using Microsoft.Extensions.Hosting;

namespace OpenMud.Mudpiler.Net.Core;

public class GameService : BackgroundService
{
    private readonly IServerGameSimulation serverGameSimulation;

    public GameService(IServerGameSimulationFactory serverGameSimulationFactory)
    {
        serverGameSimulation = serverGameSimulationFactory.Create();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var looper = new LogicLooper(60);
        await looper.RegisterActionAsync((in LogicLooperActionContext ctx) =>
        {
            serverGameSimulation.Update(new ServerFrame(ctx.CurrentFrame,
                (float)ctx.ElapsedTimeFromPreviousFrame.TotalSeconds));
            return true;
        });
    }
}