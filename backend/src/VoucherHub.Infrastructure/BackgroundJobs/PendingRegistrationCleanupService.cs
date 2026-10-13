using VoucherHub.Application.Identity;
using VoucherHub.Application.Identity.Ports;
using VoucherHub.Infrastructure.Identity;
using VoucherHub.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoucherHub.Infrastructure.Persistence;

namespace VoucherHub.Infrastructure.BackgroundJobs;

public sealed class PendingRegistrationCleanupService(
    IServiceScopeFactory scopes, IOptions<RegistrationOptions> options,
    TimeProvider clock, ILogger<PendingRegistrationCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1), clock);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<VoucherHubDbContext>();
                var cutoff = clock.GetUtcNow().UtcDateTime
                    .AddHours(-options.Value.PendingRegistrationRetentionHours);
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM pending_registrations
                    WHERE id IN (
                        SELECT id FROM pending_registrations
                        WHERE created_at < {cutoff}
                        FOR UPDATE SKIP LOCKED
                    )
                    """, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Pending registration cleanup failed");
            }
            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }
}
