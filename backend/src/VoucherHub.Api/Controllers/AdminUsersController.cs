using VoucherHub.Application.Common.Security;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoucherHub.Application.Campaigns;
using VoucherHub.Application.Users;

namespace VoucherHub.Api.Controllers;

public sealed class BlockUserRequest
{
    [Required, MaxLength(500)] public string Reason { get; init; } = string.Empty;
}

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/admin")]
public sealed class AdminUsersController(IAdminUsersService admin,
    IUserAccess access) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string? email = null,
        [FromQuery] string? status = null, CancellationToken cancellationToken = default)
    {
        if (!await IsAdmin(cancellationToken)) return Forbid();
        if (!ValidPage(page, pageSize) || email?.Length > 255 ||
            (status is not null && status is not ("ACTIVE" or "BLOCKED")))
            return BadRequest(new { code = "INVALID_FILTER" });
        return Ok(await admin.ListUsersAsync(page, pageSize,
            email?.Trim().ToLowerInvariant(), status, cancellationToken));
    }

    [HttpPost("users/{userId:guid}/block")]
    public async Task<IActionResult> Block(Guid userId, BlockUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Unauthorized();
        var result = await admin.BlockAsync(actorId, userId, request.Reason, cancellationToken);
        return result.Status switch
        {
            BlockStatus.Success => Ok(result.Receipt),
            BlockStatus.Forbidden => Forbid(),
            BlockStatus.NotFound => NotFound(new { code = result.Code }),
            BlockStatus.Conflict => Conflict(new { code = result.Code }),
            _ => BadRequest(new { code = result.Code })
        };
    }

    [HttpGet("user-vouchers")]
    public async Task<IActionResult> Issued([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] Guid? userId = null,
        [FromQuery] Guid? campaignId = null, [FromQuery] Guid? offerId = null,
        [FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!await IsAdmin(cancellationToken)) return Forbid();
        if (!ValidPage(page, pageSize) || InvalidRange(from, to))
            return BadRequest(new { code = "INVALID_FILTER" });
        return Ok(await admin.IssuedAsync(page, pageSize, userId, campaignId, offerId,
            from?.UtcDateTime, to?.UtcDateTime, cancellationToken));
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> Audit([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] Guid? actorUserId = null,
        [FromQuery] string? action = null, [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null, [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null, CancellationToken cancellationToken = default)
    {
        if (!await IsAdmin(cancellationToken)) return Forbid();
        if (!ValidPage(page, pageSize) || InvalidRange(from, to) ||
            action?.Length > 100 || entityType?.Length > 100)
            return BadRequest(new { code = "INVALID_FILTER" });
        return Ok(await admin.AuditAsync(page, pageSize, actorUserId,
            action?.Trim(), entityType?.Trim(), entityId,
            from?.UtcDateTime, to?.UtcDateTime, cancellationToken));
    }

    private bool TryActor(out Guid id) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
    private async Task<bool> IsAdmin(CancellationToken ct) =>
        TryActor(out var id) && await access.IsActiveAdminAsync(id, ct);
    private static bool ValidPage(int page, int size) =>
        page > 0 && size is >= 1 and <= 100 && page <= int.MaxValue / size;
    private static bool InvalidRange(DateTimeOffset? from, DateTimeOffset? to) =>
        from.HasValue && to.HasValue && from >= to;
}
