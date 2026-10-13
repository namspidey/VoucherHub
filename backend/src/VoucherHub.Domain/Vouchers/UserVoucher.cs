namespace VoucherHub.Domain.Entities;

public class UserVoucher
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid VoucherOfferId { get; set; }
    public string Code { get; set; } = string.Empty;
    public long PointsSpent { get; set; }
    public string RequestKey { get; set; } = string.Empty;
    public DateTime ClaimedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
