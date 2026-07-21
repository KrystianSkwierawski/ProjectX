
namespace Assets.Scripts.Areas.Shared.Enums
{
    public enum AudioTypeEnum : byte
    {
        None,

        #region Sfx

        CastingFailed,
        FireballCast,
        FireballImpact,
        FireballPrecast,
        ArrowCast,
        ArrowImpact,
        ArrowPrecast,
        SwordImpact,
        InventoryOpen,
        InventoryClose,
        LevelUp,
        QuestAccepted,
        QuestCompleted,
        FishCast,
        CanFishOut,
        FishReelIn,
        FishingBobber,
        AddItem,
        Mining,
        MinedOre,
        Herbalism,
        // TODO Lumberjack, 
        Death,
        MonsterAggro,
        MonsterAttack,
        CookingPrepare,
        CookingComplete,
        Blacksmithing,
        Alchemy,
        Currency,
        Drinking,
        SendMessage,
        Wear,

        #endregion

        #region Music

        BacgroundMusic,
        BacgroundMusic2,

        #endregion
    }
}