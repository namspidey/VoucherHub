namespace VoucherHub.Application.Common.Persistence;
public interface IDatabaseReadiness
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
