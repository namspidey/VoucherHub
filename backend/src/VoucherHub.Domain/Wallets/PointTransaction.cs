namespace VoucherHub.Domain.Entities;

public class PointTransaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public string Type { get; set; } = string.Empty;
    public long Amount { get; set; }
    public long BalanceAfter { get; set; }
    public Guid? UserVoucherId { get; set; }
    public Guid PerformedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
