namespace VoucherHub.Application.Common.Persistence;
public interface IWriteTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
public sealed class StorageConflictException(string constraintName, Exception inner) : Exception("Unique constraint conflict.", inner)
{
    public string ConstraintName { get; } = constraintName;
}
