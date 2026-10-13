using System.Text.Json;
using VoucherHub.Application.Campaigns.Ports;
using VoucherHub.Application.Common.Security;
using VoucherHub.Application.Campaigns;
using VoucherHub.Domain.Entities;

namespace VoucherHub.Application.Campaigns.UseCases;

public sealed class CampaignService(
    ICampaignStore store, TimeProvider timeProvider) : ICampaignService
{
    public Task<CampaignMutationResult> UpdateAsync(
        Guid actorId, Guid campaignId, CreateCampaignCommand command,
        CancellationToken cancellationToken = default) =>
        MutateAsync(actorId, campaignId, async campaign =>
        {
            if (campaign.Status != "DRAFT")
                return new(CampaignMutationStatus.Conflict, Error: "Only Draft campaigns can be edited.");

            var name = command.Name?.Trim();
            var description = command.Description?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200 || description?.Length > 2000 ||
                command.MaxRedemptionsPerUser <= 0 || command.StartsAt == default ||
                command.EndsAt == default || command.EndsAt <= command.StartsAt)
                return new(CampaignMutationStatus.InvalidInput, Error: "Invalid campaign configuration.");

            var endsAt = command.EndsAt.UtcDateTime;
            if (await store.HasInvalidExpiryAsync(campaignId, endsAt, cancellationToken))
                return new(CampaignMutationStatus.InvalidInput,
                    Error: "All offer expiry times must remain after the campaign end time.");

            var before = ToDetails(campaign);
            campaign.Name = name;
            campaign.Description = string.IsNullOrEmpty(description) ? null : description;
            campaign.StartsAt = command.StartsAt.UtcDateTime;
            campaign.EndsAt = endsAt;
            campaign.MaxRedemptionsPerUser = command.MaxRedemptionsPerUser;
            campaign.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
            AddAudit(actorId, "CAMPAIGN_UPDATED", "Campaign", campaign.Id,
                new { before, after = ToDetails(campaign) });
            return new(CampaignMutationStatus.Success, ToDetails(campaign));
        }, cancellationToken);

    public Task<CampaignMutationResult> SaveOfferAsync(
        Guid actorId, Guid campaignId, Guid? offerId, SaveVoucherOfferCommand command,
        CancellationToken cancellationToken = default) =>
        MutateAsync(actorId, campaignId, async campaign =>
        {
            if (campaign.Status != "DRAFT")
                return new(CampaignMutationStatus.Conflict, Error: "Offers can only be changed in Draft campaigns.");

            var name = command.Name?.Trim();
            var description = command.Description?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200 || description?.Length > 2000 ||
                command.PointsCost <= 0 || command.TotalQuantity <= 0 ||
                command.ExpiresAt == default || command.ExpiresAt.UtcDateTime <= campaign.EndsAt)
                return new(CampaignMutationStatus.InvalidInput,
                    Error: "Offer requires a name, positive price/quantity and expiry after campaign end.");

            var offer = offerId.HasValue
                ? await store.GetOfferAsync(campaignId, offerId.Value, cancellationToken)
                : null;
            if (offerId.HasValue && offer is null)
                return new(CampaignMutationStatus.NotFound, Error: "Offer was not found in this campaign.");
            if (offer is not null && offer.IssuedQuantity != 0)
                return new(CampaignMutationStatus.Conflict, Error: "An offer with issued vouchers cannot be edited.");

            var before = offer is null ? null : ToOfferDetails(offer);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (offer is null)
            {
                offer = new VoucherOffer
                {
                    Id = Guid.NewGuid(), CampaignId = campaignId,
                    IssuedQuantity = 0, CreatedAt = now
                };
                store.Add(offer);
            }
            offer.Name = name;
            offer.Description = string.IsNullOrEmpty(description) ? null : description;
            offer.PointsCost = command.PointsCost;
            offer.TotalQuantity = command.TotalQuantity;
            offer.ExpiresAt = command.ExpiresAt.UtcDateTime;
            offer.UpdatedAt = now;
            campaign.UpdatedAt = now;
            AddAudit(actorId, offerId.HasValue ? "VOUCHER_OFFER_UPDATED" : "VOUCHER_OFFER_CREATED",
                "VoucherOffer", offer.Id, new { before, after = ToOfferDetails(offer) });
            return new(CampaignMutationStatus.Success, Offer: ToOfferDetails(offer));
        }, cancellationToken);

    public Task<CampaignMutationResult> PublishAsync(
        Guid actorId, Guid campaignId, CancellationToken cancellationToken = default) =>
        MutateAsync(actorId, campaignId, async campaign =>
        {
            if (campaign.Status != "DRAFT")
                return new(CampaignMutationStatus.Conflict, Error: "Only Draft campaigns can be published.");

            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (string.IsNullOrWhiteSpace(campaign.Name) || campaign.MaxRedemptionsPerUser <= 0 ||
                campaign.EndsAt <= now || campaign.EndsAt <= campaign.StartsAt)
                return new(CampaignMutationStatus.InvalidInput,
                    Error: "Publish requires a valid campaign that has not ended.");

            var offers = await store.GetOffersAsync(campaignId, cancellationToken);
            if (offers.Count == 0 || offers.Any(x => string.IsNullOrWhiteSpace(x.Name) ||
                x.PointsCost <= 0 || x.TotalQuantity <= 0 || x.IssuedQuantity != 0 ||
                x.ExpiresAt <= campaign.EndsAt))
                return new(CampaignMutationStatus.InvalidInput,
                    Error: "Publish requires at least one valid offer with expiry after campaign end.");

            var before = ToDetails(campaign);
            campaign.Status = "PUBLISHED";
            campaign.UpdatedAt = now;
            AddAudit(actorId, "CAMPAIGN_PUBLISHED", "Campaign", campaign.Id,
                new { before, after = ToDetails(campaign) });
            return new(CampaignMutationStatus.Success, ToDetails(campaign));
        }, cancellationToken);

    public Task<CampaignMutationResult> CancelAsync(
        Guid actorId, Guid campaignId, CancellationToken cancellationToken = default) =>
        MutateAsync(actorId, campaignId, campaign =>
        {
            // Repeat cancellation returns the current state without duplicating the audit.
            if (campaign.Status == "CANCELLED")
                return Task.FromResult(new CampaignMutationResult(CampaignMutationStatus.Success, ToDetails(campaign)));
            if (campaign.Status is not ("DRAFT" or "PUBLISHED"))
                return Task.FromResult(new CampaignMutationResult(CampaignMutationStatus.Conflict,
                    Error: "Campaign cannot be cancelled from its current status."));

            var before = ToDetails(campaign);
            campaign.Status = "CANCELLED";
            campaign.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
            AddAudit(actorId, "CAMPAIGN_CANCELLED", "Campaign", campaign.Id,
                new { before, after = ToDetails(campaign) });
            return Task.FromResult(new CampaignMutationResult(CampaignMutationStatus.Success, ToDetails(campaign)));
        }, cancellationToken);

    public async Task<IReadOnlyList<VoucherOfferDetails>> GetOffersAsync(Guid campaignId, CancellationToken cancellationToken = default) =>
        (await store.GetOffersAsync(campaignId, cancellationToken)).Select(ToOfferDetails).ToList();

    private async Task<CampaignMutationResult> MutateAsync(
        Guid actorId, Guid campaignId, Func<Campaign, Task<CampaignMutationResult>> mutation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await store.BeginAsync(cancellationToken);
        // Lock order for admin campaign writes: actor -> campaign -> offers.
        var actor = await store.LockActorAsync(actorId, cancellationToken);
        if (actor is null || actor.Role != "ADMIN" || actor.Status != "ACTIVE")
            return new(CampaignMutationStatus.Forbidden);

        var campaign = await store.LockCampaignAsync(campaignId, cancellationToken);
        if (campaign is null)
            return new(CampaignMutationStatus.NotFound, Error: "Campaign was not found.");

        var result = await mutation(campaign);
        if (result.Status != CampaignMutationStatus.Success)
            return result;

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private void AddAudit(Guid actorId, string action, string entityType, Guid entityId, object details) =>
        store.Add(new AuditLog
        {
            Id = Guid.NewGuid(), ActorUserId = actorId, Action = action,
            EntityType = entityType, EntityId = entityId,
            Details = JsonSerializer.Serialize(details), CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        });

    private static VoucherOfferDetails ToOfferDetails(VoucherOffer offer) => new(
        offer.Id, offer.CampaignId, offer.Name, offer.Description, offer.PointsCost,
        offer.TotalQuantity, offer.IssuedQuantity, offer.ExpiresAt, offer.CreatedAt, offer.UpdatedAt);

    public async Task<CreateCampaignResult> CreateAsync(
        Guid actorId, CreateCampaignCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await store.BeginAsync(cancellationToken);

        // Hold a shared lock until commit so blocking/demoting the actor cannot race this write.
        var actor = await store.LockActorAsync(actorId, cancellationToken);

        if (actor is null || actor.Role != "ADMIN" || actor.Status != "ACTIVE")
            return new(CreateCampaignStatus.Forbidden);

        var name = command.Name?.Trim();
        var description = command.Description?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200 ||
            description?.Length > 2000 || command.MaxRedemptionsPerUser <= 0 ||
            command.StartsAt == default || command.EndsAt == default ||
            command.EndsAt <= command.StartsAt)
            return new(CreateCampaignStatus.InvalidInput, Error:
                "Name, time range or campaign redemption quota is invalid.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var campaign = new Campaign
        {
            Id = Guid.NewGuid(), Name = name,
            Description = string.IsNullOrEmpty(description) ? null : description,
            StartsAt = command.StartsAt.UtcDateTime,
            EndsAt = command.EndsAt.UtcDateTime,
            Status = "DRAFT", MaxRedemptionsPerUser = command.MaxRedemptionsPerUser,
            CreatedBy = actorId, CreatedAt = now, UpdatedAt = now
        };

        store.Add(campaign);
        store.Add(new AuditLog
        {
            Id = Guid.NewGuid(), ActorUserId = actorId,
            Action = "CAMPAIGN_CREATED", EntityType = "Campaign", EntityId = campaign.Id,
            Details = JsonSerializer.Serialize(ToDetails(campaign)), CreatedAt = now
        });

        // EF saves the campaign and its audit in the same transaction.
        await transaction.CommitAsync(cancellationToken);
        return new(CreateCampaignStatus.Success, ToDetails(campaign));
    }

    public async Task<CampaignDetails?> GetAsync(
        Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await store.GetAsync(campaignId, cancellationToken);
        return campaign is null ? null : ToDetails(campaign);
    }

    private static CampaignDetails ToDetails(Campaign campaign) => new(
        campaign.Id, campaign.Name, campaign.Description, campaign.StartsAt, campaign.EndsAt,
        campaign.Status, campaign.MaxRedemptionsPerUser, campaign.CreatedBy,
        campaign.CreatedAt, campaign.UpdatedAt);
}
