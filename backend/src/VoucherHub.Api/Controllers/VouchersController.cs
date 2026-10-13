using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Hybrid;
using VoucherHub.Application.Vouchers;

namespace VoucherHub.Api.Controllers;

[ApiController]
[Authorize]
public sealed class VouchersController(IRedemptionService redemptions,
    HybridCache cache, ILogger<VouchersController> logger) : ControllerBase
{
    [HttpPost("api/voucher-offers/{offerId:guid}/redeem")]
    [EnableRateLimiting("redeem")]
    public async Task<IActionResult> Redeem(Guid offerId, CancellationToken cancellationToken)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        var value = Request.Headers["Idempotency-Key"].ToString();
        if (!Guid.TryParse(value, out var key) || key == Guid.Empty)
            return BadRequest(new { code = "INVALID_REQUEST_KEY" });
        var result = await redemptions.RedeemAsync(userId, offerId,
            key.ToString("D"), cancellationToken);
        if (result.Status == RedeemStatus.Created)
        {
            try { await cache.RemoveByTagAsync("campaigns", cancellationToken); }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Campaign cache invalidation failed after redemption");
            }
        }
        return result.Status switch
        {
            RedeemStatus.Created => CreatedAtAction(nameof(GetMine),
                new { id = result.Voucher!.Id }, result.Voucher),
            RedeemStatus.Replayed => Ok(result.Voucher),
            RedeemStatus.Forbidden => Forbid(),
            RedeemStatus.NotFound => NotFound(new { code = result.Code }),
            _ => Conflict(new { code = result.Code })
        };
    }

    [HttpGet("api/users/me/vouchers")]
    public async Task<IActionResult> Mine([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] bool? expired = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize)
            return BadRequest(new { code = "INVALID_PAGING" });
        return Ok(await redemptions.MineAsync(userId, page, pageSize,
            expired, cancellationToken));
    }

    [HttpGet("api/users/me/vouchers/{id:guid}")]
    public async Task<IActionResult> GetMine(Guid id, CancellationToken cancellationToken)
    {
        if (!TryUser(out var userId)) return Unauthorized();
        var voucher = await redemptions.GetMineAsync(userId, id, cancellationToken);
        return voucher is null ? NotFound() : Ok(voucher);
    }

    private bool TryUser(out Guid id) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
}
