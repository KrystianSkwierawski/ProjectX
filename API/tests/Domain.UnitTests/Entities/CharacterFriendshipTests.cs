using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Domain.UnitTests.Entities;

public class CharacterFriendshipTests
{
    [Fact]
    public void Create_NormalizesCharacterOrderAndKeepsRequester()
    {
        var friendship = CharacterFriendship.Create(42, 7);

        Assert.Equal(7, friendship.FirstCharacterId);
        Assert.Equal(42, friendship.SecondCharacterId);
        Assert.Equal(42, friendship.RequestedByCharacterId);
        Assert.Equal(FriendshipStatusEnum.Pending, friendship.Status);
        Assert.Equal(7, friendship.GetOtherCharacterId(42));
    }

    [Fact]
    public void Accept_AllowsOnlyInvitationRecipient()
    {
        var friendship = CharacterFriendship.Create(42, 7);

        Assert.Throws<InvalidOperationException>(() => friendship.Accept(42));

        friendship.Accept(7);

        Assert.Equal(FriendshipStatusEnum.Accepted, friendship.Status);
    }

    [Theory]
    [InlineData(0, 7)]
    [InlineData(7, 0)]
    public void Create_RejectsInvalidCharacterIds(int requesterId, int targetId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CharacterFriendship.Create(requesterId, targetId));
    }

    [Fact]
    public void Create_RejectsSelfInvitation()
    {
        Assert.Throws<InvalidOperationException>(() => CharacterFriendship.Create(7, 7));
    }
}
