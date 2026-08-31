using FinTrack.Application.Features.RecurringTransactions.Processing;

namespace FinTrack.Api.BackgroundServices;

/// <summary>
/// Periodically materializes due recurring rules into transactions. Generation logic lives in
/// <see cref="IRecurringTransactionProcessor"/> (in the Application layer) so it can be tested
/// directly; this worker only handles scheduling and scope creation.
/// </summary>
public class RecurringTransactionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecurringTransactionWorker> _logger;
    private readonly bool _enabled;
    private readonly TimeSpan _interval;

    public RecurringTransactionWorker(
        IServiceProvider serviceProvider,
        ILogger<RecurringTransactionWorker> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _enabled = configuration.GetValue<bool?>("RecurringTransactions:Enabled") ?? true;
        var intervalSeconds = configuration.GetValue<int?>("RecurringTransactions:IntervalSeconds") ?? 3600;
        _interval = TimeSpan.FromSeconds(Math.Max(5, intervalSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Recurring transaction worker is disabled by configuration.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IRecurringTransactionProcessor>();
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var created = await processor.ProcessDueAsync(today, stoppingToken);
                if (created > 0)
                {
                    _logger.LogInformation("Generated {Count} transaction(s) from recurring rules.", created);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(exception, "Recurring transaction processing failed.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
