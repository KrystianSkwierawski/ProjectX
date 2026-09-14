namespace Assets.Scripts.Areas.Trade.Enums
{
    public enum TradeOperationStatusEnum
    {
        Applied,
        CannotTradeSelf,
        FriendRequired,
        TargetOffline,
        TargetBusy,
        InvitationAlreadyPending,
        InvitationNotFound,
        TradeNotFound,
        OfferLocked,
        InvalidOffer,
        OfferLimitReached,
        WaitingForLocks,
        InventoryFull,
        ItemsUnavailable,
        InventoryChanged,
        RequestFailed,
        CommitRetrying
    }
}
