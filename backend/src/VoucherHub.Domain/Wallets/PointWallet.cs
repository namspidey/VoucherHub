namespace VoucherHub.Domain.Entities;

public class PointWallet
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public long Balance { get; set; }

    public DateTime UpdatedAt { get; set; }
}
