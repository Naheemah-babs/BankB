
using TatumWelcome2.BLL;
using TatumWelcome2.Interfaces;

namespace TatumWelcome2.Services;

public class BillPaymentReconciliationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BillPaymentReconciliationService> _logger;

    public BillPaymentReconciliationService(
        IServiceScopeFactory scopeFactory,
        ILogger<BillPaymentReconciliationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using PeriodicTimer timer =
            new(TimeSpan.FromSeconds(5));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (BillPurchaseService.PendingCount == 0)
                    continue;

                try
                {
                    using IServiceScope scope =
                        _scopeFactory.CreateScope();

                    IBankApiClient bankApiClient =
                        scope.ServiceProvider
                            .GetRequiredService<IBankApiClient>();

                    await BillPurchaseService.ReconcilePendingAsync(
                        bankApiClient,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Unexpected error during bill payment reconciliation.");
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            //  application shutdown.
        }
    }
}
