namespace VoucherHub.Application.Campaigns;

public sealed record CreateCampaignCommand(
    string Name,
    string? Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int MaxRedemptionsPerUser);

public sealed record CampaignDetails(
    Guid Id,
    string Name,
    string? Description,
    DateTime StartsAt,
    DateTime EndsAt,
    string Status,
    int MaxRedemptionsPerUser,
    Guid CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public enum CreateCampaignStatus { Success, Forbidden, InvalidInput }

public sealed record CreateCampaignResult(
    CreateCampaignStatus Status,
    CampaignDetails? Campaign = null,
    string? Error = null);

public sealed record SaveVoucherOfferCommand(
    string Name, string? Description, long PointsCost,
    int TotalQuantity, DateTimeOffset ExpiresAt);

public sealed record VoucherOfferDetails(
    Guid Id, Guid CampaignId, string Name, string? Description,
    long PointsCost, int TotalQuantity, int IssuedQuantity,
    DateTime ExpiresAt, DateTime CreatedAt, DateTime UpdatedAt);

public enum CampaignMutationStatus { Success, Forbidden, NotFound, InvalidInput, Conflict }

public sealed record CampaignMutationResult(
    CampaignMutationStatus Status,
    CampaignDetails? Campaign = null,
    VoucherOfferDetails? Offer = null,
    string? Error = null);

public interface ICampaignService
{
    Task<CampaignMutationResult> UpdateAsync(Guid actorId, Guid campaignId,
        CreateCampaignCommand command, CancellationToken cancellationToken = default);

    Task<CampaignMutationResult> SaveOfferAsync(Guid actorId, Guid campaignId,
        Guid? offerId, SaveVoucherOfferCommand command, CancellationToken cancellationToken = default);

    Task<CampaignMutationResult> PublishAsync(Guid actorId, Guid campaignId,
        CancellationToken cancellationToken = default);

    Task<CampaignMutationResult> CancelAsync(Guid actorId, Guid campaignId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VoucherOfferDetails>> GetOffersAsync(Guid campaignId,
        CancellationToken cancellationToken = default);

    Task<CreateCampaignResult> CreateAsync(
        Guid actorId, CreateCampaignCommand command,
        CancellationToken cancellationToken = default);

    Task<CampaignDetails?> GetAsync(
        Guid campaignId, CancellationToken cancellationToken = default);
}
