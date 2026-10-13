namespace VoucherHub.Domain.Entities;

public class VoucherOffer
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long PointsCost { get; set; }
    public int TotalQuantity { get; set; }
    public int IssuedQuantity { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
