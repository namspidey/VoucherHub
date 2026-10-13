using Microsoft.AspNetCore.Identity;
using VoucherHub.Application.Identity.Ports;
using VoucherHub.Domain.Entities;
namespace VoucherHub.Infrastructure.Identity;
public sealed class PendingPasswordService(IPasswordHasher<PendingRegistration> hasher) : IPendingPasswordService
{
    public string Hash(PendingRegistration pending, string password) => hasher.HashPassword(pending, password);
}
