using VoucherHub.Application.Common.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using VoucherHub.Application.Campaigns;

namespace VoucherHub.Api.Controllers;

[ApiController]
[Route("api/campaigns")]
public sealed class CampaignsController(ICampaignCatalog catalog, HybridCache cache) : ControllerBase
{
    private static readonly HybridCacheEntryOptions StockTtl = new()
    {
        Expiration = TimeSpan.FromSeconds(2),
        LocalCacheExpiration = TimeSpan.FromSeconds(2)
    };

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string? phase = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize ||
            (phase is not null && phase is not ("UPCOMING" or "ONGOING" or "ENDED")))
            return BadRequest(new { code = "INVALID_FILTER" });
        var result = await cache.GetOrCreateAsync(
            $"campaign-list:{page}:{pageSize}:{phase ?? "ALL"}",
            ct => new ValueTask<Page<CatalogCampaign>>(
                catalog.PublicListAsync(page, pageSize, phase, ct)),
            StockTtl, ["campaigns"], cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await cache.GetOrCreateAsync(
            $"campaign-detail:{id}",
            ct => new ValueTask<CatalogCampaign?>(catalog.PublicGetAsync(id, ct)),
            StockTtl, ["campaigns", $"campaign:{id}"], cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
