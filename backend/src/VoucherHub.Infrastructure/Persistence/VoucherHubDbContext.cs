using Microsoft.EntityFrameworkCore;
using VoucherHub.Domain.Entities;

namespace VoucherHub.Infrastructure.Persistence;

public class VoucherHubDbContext : DbContext
{
    public VoucherHubDbContext(DbContextOptions<VoucherHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PendingRegistration> PendingRegistrations => Set<PendingRegistration>();
    public DbSet<PointWallet> PointWallets => Set<PointWallet>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<VoucherOffer> VoucherOffers => Set<VoucherOffer>();
    public DbSet<UserVoucher> UserVouchers => Set<UserVoucher>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VoucherHubDbContext).Assembly);
    }
}
