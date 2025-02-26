namespace OrderProcessor;

public class OrderProcessingWorker(ILogger<OrderProcessingWorker> logger,
                                   Instrumentation instrumentation,
                                   IServiceScopeFactory serviceScopeFactory)
    : BackgroundService
{
    private TimeSpan CheckOrderInterval => TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var activity = instrumentation.ActivitySource.StartActivity("order-processor.worker"))
            {
                logger.LogInformation($"Worker running at: {DateTime.UtcNow}");

                await using var scope = serviceScopeFactory.CreateAsyncScope();

                var request = scope.ServiceProvider.GetRequiredService<OrderProcessingRequest>();

                try
                {
                    await request.ProcessOrdersAsync(activity, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // We're shutting down
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting orders");
                }
            }

            await Task.Delay(CheckOrderInterval, stoppingToken);
        }
    }
}
