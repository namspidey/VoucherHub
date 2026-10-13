using VoucherHub.Application.Common.Persistence;
namespace VoucherHub.Infrastructure.Persistence;
public sealed class DatabaseReadiness(VoucherHubDbContext db) : IDatabaseReadiness
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        db.Database.CanConnectAsync(cancellationToken);
}
