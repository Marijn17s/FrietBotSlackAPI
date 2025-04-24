using FrietBot.Jobs;

namespace FrietBot;

public class SchedulerHostedService(SchedulerService schedulerService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await schedulerService.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await schedulerService.StopAsync();
    }
}