using VoucherHub.Application.Common.Pagination;
using VoucherHub.Application.Users.Ports;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VoucherHub.Application.Users;
using VoucherHub.Domain.Entities;
using VoucherHub.Infrastructure.Persistence;

namespace VoucherHub.Infrastructure.Persistence.Identity;

public sealed class AdminUserQueries(VoucherHubDbContext db)
    : IAdminUserQueries
{
    public async Task<Page<AdminUserSummary>> ListUsersAsync(int page, int pageSize,
        string? email, string? status, CancellationToken cancellationToken = default)
    {
        var query = from user in db.Users.AsNoTracking()
            join wallet in db.PointWallets.AsNoTracking() on user.Id equals wallet.UserId
            select new { user, wallet };
        if (status is not null) query = query.Where(x => x.user.Status == status);
        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(x => EF.Functions.ILike(x.user.Email, $"%{email}%"));
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.user.CreatedAt)
            .ThenByDescending(x => x.user.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AdminUserSummary(x.user.Id, x.user.Email,
                x.user.DisplayName, x.user.Role, x.user.Status, x.wallet.Balance,
                x.user.CreatedAt, x.user.UpdatedAt)).ToListAsync(cancellationToken);
        return new(rows, page, pageSize, count);
    }

    public async Task<Page<IssuedVoucherEntry>> IssuedAsync(int page, int pageSize,
        Guid? userId, Guid? campaignId, Guid? offerId, DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = from voucher in db.UserVouchers.AsNoTracking()
            join offer in db.VoucherOffers.AsNoTracking()
                on voucher.VoucherOfferId equals offer.Id
            select new { voucher, offer };
        if (userId.HasValue) query = query.Where(x => x.voucher.UserId == userId);
        if (campaignId.HasValue) query = query.Where(x => x.offer.CampaignId == campaignId);
        if (offerId.HasValue) query = query.Where(x => x.offer.Id == offerId);
        if (from.HasValue) query = query.Where(x => x.voucher.ClaimedAt >= from);
        if (to.HasValue) query = query.Where(x => x.voucher.ClaimedAt < to);
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.voucher.ClaimedAt)
            .ThenByDescending(x => x.voucher.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new IssuedVoucherEntry(x.voucher.Id, x.voucher.UserId,
                x.offer.Id, x.offer.CampaignId, x.voucher.PointsSpent,
                x.voucher.ClaimedAt, x.voucher.ExpiresAt)).ToListAsync(cancellationToken);
        return new(rows, page, pageSize, count);
    }

    public async Task<Page<AuditEntry>> AuditAsync(int page, int pageSize,
        Guid? actorId, string? action, string? entityType, Guid? entityId,
        DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (actorId.HasValue) query = query.Where(x => x.ActorUserId == actorId);
        if (action is not null) query = query.Where(x => x.Action == action);
        if (entityType is not null) query = query.Where(x => x.EntityType == entityType);
        if (entityId.HasValue) query = query.Where(x => x.EntityId == entityId);
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from);
        if (to.HasValue) query = query.Where(x => x.CreatedAt < to);
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AuditEntry(x.Id, x.ActorUserId, x.Action,
                x.EntityType, x.EntityId, x.Details, x.CreatedAt)).ToListAsync(cancellationToken);
        return new(rows, page, pageSize, count);
    }
}
