using VoucherHub.Application.Common.Security;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using VoucherHub.Api.Contracts.Campaigns;
using VoucherHub.Application.Campaigns;

namespace VoucherHub.Api.Controllers;

[ApiController]
[Route("api/admin/campaigns")]
[Authorize(Roles = "ADMIN")]
public sealed class AdminCampaignsController(ICampaignService campaignService,
    ICampaignCatalog catalog, IUserAccess access, HybridCache cache,
    ILogger<AdminCampaignsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string? status = null,
        [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        if (!await access.IsActiveAdminAsync(actorId, cancellationToken)) return Forbid();
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize ||
            (status is not null && status is not ("DRAFT" or "PUBLISHED" or "CANCELLED")) ||
            search?.Length > 200)
            return BadRequest(new { code = "INVALID_FILTER" });
        return Ok(await catalog.AdminListAsync(page, pageSize, status,
            search?.Trim(), cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CreateCampaignRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        return await MutationResponseAsync(await campaignService.UpdateAsync(actorId, id, new(
            request.Name, request.Description, request.StartsAt, request.EndsAt,
            request.MaxRedemptionsPerUser), cancellationToken));
    }

    [HttpPost("{id:guid}/offers")]
    public async Task<IActionResult> CreateOffer(Guid id, SaveVoucherOfferRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        var result = await campaignService.SaveOfferAsync(actorId, id, null, ToCommand(request), cancellationToken);
        if (result.Status == CampaignMutationStatus.Success)
            await InvalidateCampaignCacheAsync();
        return result.Status == CampaignMutationStatus.Success
            ? CreatedAtAction(nameof(GetOffers), new { id }, result.Offer)
            : MutationResponse(result);
    }

    [HttpPut("{id:guid}/offers/{offerId:guid}")]
    public async Task<IActionResult> UpdateOffer(Guid id, Guid offerId,
        SaveVoucherOfferRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        return await MutationResponseAsync(await campaignService.SaveOfferAsync(
            actorId, id, offerId, ToCommand(request), cancellationToken));
    }

    [HttpGet("{id:guid}/offers")]
    public async Task<IActionResult> GetOffers(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        if (!await access.IsActiveAdminAsync(actorId, cancellationToken)) return Forbid();
        if (await campaignService.GetAsync(id, cancellationToken) is null) return NotFound();
        return Ok(await campaignService.GetOffersAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        return await MutationResponseAsync(await campaignService.PublishAsync(actorId, id, cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorId)) return Unauthorized();
        return await MutationResponseAsync(await campaignService.CancelAsync(actorId, id, cancellationToken));
    }

    private bool TryGetActorId(out Guid actorId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);

    private static SaveVoucherOfferCommand ToCommand(SaveVoucherOfferRequest request) => new(
        request.Name, request.Description, request.PointsCost, request.TotalQuantity, request.ExpiresAt);

    private IActionResult MutationResponse(CampaignMutationResult result) => result.Status switch
    {
        CampaignMutationStatus.Success => Ok((object?)result.Offer ?? result.Campaign),
        CampaignMutationStatus.Forbidden => Forbid(),
        CampaignMutationStatus.NotFound => NotFound(new { message = result.Error }),
        CampaignMutationStatus.Conflict => Conflict(new { message = result.Error }),
        _ => BadRequest(new { message = result.Error })
    };

    private async Task<IActionResult> MutationResponseAsync(CampaignMutationResult result)
    {
        if (result.Status == CampaignMutationStatus.Success)
            await InvalidateCampaignCacheAsync();
        return MutationResponse(result);
    }

    private async Task InvalidateCampaignCacheAsync()
    {
        try { await cache.RemoveByTagAsync("campaigns", HttpContext.RequestAborted); }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Campaign cache invalidation failed after mutation");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return Unauthorized();

        var result = await campaignService.CreateAsync(actorId, new(
            request.Name, request.Description, request.StartsAt, request.EndsAt,
            request.MaxRedemptionsPerUser), cancellationToken);

        if (result.Status == CreateCampaignStatus.Success)
            await InvalidateCampaignCacheAsync();

        return result.Status switch
        {
            CreateCampaignStatus.Success => CreatedAtAction(nameof(GetById),
                new { id = result.Campaign!.Id }, result.Campaign),
            CreateCampaignStatus.Forbidden => Forbid(),
            _ => BadRequest(new { message = result.Error })
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return Unauthorized();
        if (!await access.IsActiveAdminAsync(actorId, cancellationToken))
            return Forbid();

        var campaign = await campaignService.GetAsync(id, cancellationToken);
        return campaign is null ? NotFound() : Ok(campaign);
    }
}
