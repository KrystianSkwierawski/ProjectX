using ProjectX.Domain.Common;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.Entities;

public class CharacterFriendship : BaseAuditableEntity
{
    public int Id { get; set; }

    public int FirstCharacterId { get; private set; }

    public int SecondCharacterId { get; private set; }

    public int RequestedByCharacterId { get; private set; }

    public FriendshipStatusEnum Status { get; private set; }

    public virtual Character FirstCharacter { get; private set; } = null!;

    public virtual Character SecondCharacter { get; private set; } = null!;

    public static CharacterFriendship Create(int requestingCharacterId, int targetCharacterId)
    {
        if (requestingCharacterId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestingCharacterId));
        }

        if (targetCharacterId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetCharacterId));
        }

        if (requestingCharacterId == targetCharacterId)
        {
            throw new InvalidOperationException("A character cannot invite itself.");
        }

        return new CharacterFriendship
        {
            FirstCharacterId = Math.Min(requestingCharacterId, targetCharacterId),
            SecondCharacterId = Math.Max(requestingCharacterId, targetCharacterId),
            RequestedByCharacterId = requestingCharacterId,
            Status = FriendshipStatusEnum.Pending
        };
    }

    public bool Includes(int characterId)
    {
        return FirstCharacterId == characterId || SecondCharacterId == characterId;
    }

    public int GetOtherCharacterId(int characterId)
    {
        if (FirstCharacterId == characterId)
        {
            return SecondCharacterId;
        }

        if (SecondCharacterId == characterId)
        {
            return FirstCharacterId;
        }

        throw new InvalidOperationException("The character is not part of this friendship.");
    }

    public void Accept(int acceptingCharacterId)
    {
        if (!Includes(acceptingCharacterId) || RequestedByCharacterId == acceptingCharacterId || Status != FriendshipStatusEnum.Pending)
        {
            throw new InvalidOperationException("Only the recipient can accept a pending friendship invitation.");
        }

        Status = FriendshipStatusEnum.Accepted;
    }
}
