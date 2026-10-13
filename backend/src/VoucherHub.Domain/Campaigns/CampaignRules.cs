using VoucherHub.Domain.Entities;
namespace VoucherHub.Domain.Campaigns;

public static class CampaignRules
{
    public static string? RedemptionError(Campaign campaign, DateTime now)
    {
        if (campaign.Status != "PUBLISHED") return "CAMPAIGN_NOT_PUBLISHED";
        if (now < campaign.StartsAt) return "CAMPAIGN_NOT_STARTED";
        if (now >= campaign.EndsAt) return "CAMPAIGN_ENDED";
        return null;
    }
}
