using System.ComponentModel.DataAnnotations;

namespace VoucherHub.Api.Contracts.Campaigns;

public sealed class CreateCampaignRequest : IValidatableObject
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset EndsAt { get; init; }

    [Range(1, int.MaxValue)]
    public int MaxRedemptionsPerUser { get; init; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartsAt == default)
            yield return new("Start time is required.", [nameof(StartsAt)]);
        if (EndsAt == default || EndsAt <= StartsAt)
            yield return new("End time must be after start time.", [nameof(EndsAt)]);
    }
}
