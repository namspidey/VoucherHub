using VoucherHub.Application.Common.Persistence;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Application.Identity.Ports;
public interface IRegistrationStore
{
    Task<IWriteTransaction> BeginAsync(CancellationToken ct);
    Task LockEmailAsync(string email, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<PendingRegistration?> LockPendingEmailAsync(string email, CancellationToken ct);
    Task<PendingRegistration?> LockPendingTokenAsync(string hash, CancellationToken ct);
    Task DeleteExpiredAsync(DateTime cutoff, CancellationToken ct);
    void Add(PendingRegistration pending);
    void Add(User user);
    void Add(PointWallet wallet);
    void Remove(PendingRegistration pending);
}
public interface IPendingPasswordService { string Hash(PendingRegistration pending, string password); }
