namespace Assets.Scripts.Enums
{
    public enum AudioTypeEnum : byte
    {
        None,

        #region Sfx

        CastingFailed,
        FireballCast,
        FireballImpact,
        FireballPrecast,
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

        #endregion

        #region Music

        BacgroundMusic,
        BacgroundMusic2,

        #endregion
    }
}