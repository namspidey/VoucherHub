using Microsoft.EntityFrameworkCore;
using VoucherHub.Application.Identity.Ports;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Infrastructure.Persistence.Identity;
public sealed class LoginStore(VoucherHubDbContext db) : ILoginStore
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct) =>
        db.Users.SingleOrDefaultAsync(x => x.Email == email, ct);
    public async Task SaveAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
