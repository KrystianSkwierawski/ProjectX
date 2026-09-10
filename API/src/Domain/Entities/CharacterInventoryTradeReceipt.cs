using ProjectX.Domain.Common;

namespace ProjectX.Domain.Entities;

public sealed class CharacterInventoryTradeReceipt : BaseAuditableEntity
{
    private CharacterInventoryTradeReceipt() { }

    public Guid Id { get; private set; }

    public int SourceCharacterId { get; private set; }

    public int TargetCharacterId { get; private set; }

    public string RequestFingerprint { get; private set; } = string.Empty;

    public static CharacterInventoryTradeReceipt Create(
        Guid id,
        int sourceCharacterId,
        int targetCharacterId,
        string requestFingerprint)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A trade receipt requires a non-empty identifier.", nameof(id));
        }

        if (sourceCharacterId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceCharacterId));
        }

        if (targetCharacterId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetCharacterId));
        }

        if (sourceCharacterId == targetCharacterId)
        {
            throw new InvalidOperationException("A trade receipt requires two different characters.");
        }

        if (string.IsNullOrWhiteSpace(requestFingerprint))
        {
            throw new ArgumentException("A trade receipt requires a request fingerprint.", nameof(requestFingerprint));
        }

        return new CharacterInventoryTradeReceipt
        {
            Id = id,
            SourceCharacterId = sourceCharacterId,
            TargetCharacterId = targetCharacterId,
            RequestFingerprint = requestFingerprint
        };
    }

    public bool Matches(int sourceCharacterId, int targetCharacterId, string requestFingerprint)
    {
        return SourceCharacterId == sourceCharacterId
            && TargetCharacterId == targetCharacterId
            && string.Equals(RequestFingerprint, requestFingerprint, StringComparison.Ordinal);
    }
}
