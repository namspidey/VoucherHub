using System.ComponentModel.DataAnnotations;

namespace VoucherHub.Api.Contracts.Campaigns;

public sealed class SaveVoucherOfferRequest : IValidatableObject
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Range(1, long.MaxValue)]
    public long PointsCost { get; init; }

    [Range(1, int.MaxValue)]
    public int TotalQuantity { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ExpiresAt == default)
            yield return new("Expiry time is required.", [nameof(ExpiresAt)]);
    }
}
