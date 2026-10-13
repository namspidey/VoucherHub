using VoucherHub.Application.Wallets;
using VoucherHub.Application.Common.Security;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoucherHub.Application.Campaigns;
using VoucherHub.Application.Users;

namespace VoucherHub.Api.Controllers;

public sealed class GrantPointsRequest
{
    [Range(1, long.MaxValue)] public long Amount { get; init; }
    [Required, MaxLength(500)] public string Reason { get; init; } = string.Empty;
}

[ApiController]
[Authorize]
public sealed class WalletsController(IWalletService wallets, IUserAccess access) : ControllerBase
{
    [HttpPost("api/admin/users/{userId:guid}/points/grants")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Grant(Guid userId, GrantPointsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Unauthorized();
        var result = await wallets.GrantAsync(actorId, userId, request.Amount,
            request.Reason, cancellationToken);
        return result.Status switch
        {
            GrantStatus.Success => StatusCode(201, result.Receipt),
            GrantStatus.Forbidden => Forbid(),
            GrantStatus.NotFound => NotFound(new { code = result.Code }),
            GrantStatus.Conflict => Conflict(new { code = result.Code }),
            _ => BadRequest(new { code = result.Code })
        };
    }

    [HttpGet("api/users/me/point-transactions")]
    public async Task<IActionResult> Mine([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryActor(out var userId)) return Unauthorized();
        return await History(userId, page, pageSize, type, cancellationToken);
    }

    [HttpGet("api/admin/users/{userId:guid}/point-transactions")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> AdminHistory(Guid userId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryActor(out var actorId)) return Unauthorized();
        // Read authorization is checked against current DB status in the service below.
        if (!await access.IsActiveAdminAsync(actorId, cancellationToken)) return Forbid();
        return await History(userId, page, pageSize, type, cancellationToken);
    }

    private async Task<IActionResult> History(Guid userId, int page, int pageSize,
        string? type, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize ||
            (type is not null && type is not ("ADMIN_GRANT" or "VOUCHER_REDEMPTION")))
            return BadRequest(new { code = "INVALID_PAGING_OR_TYPE" });
        var result = await wallets.HistoryAsync(userId, page, pageSize, type, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private bool TryActor(out Guid id) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);

}
