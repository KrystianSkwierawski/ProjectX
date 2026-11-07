// Uses the production trade state. Engine attributes are stubbed; this does not
// exercise HTTP persistence, NGO, UniTask's player loop or authenticated disconnects.
using System;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Trade.Enums;
using Assets.Scripts.Areas.Trade.Mono;

internal static class TradeQuestRegression
{
    private const ulong FirstClientId = 71;
    private const ulong SecondClientId = 72;

    public static void Run()
    {
        ProgressBeforeLockParticipatesInDrain();
        ProgressAfterDrainWaitsForCommitAndRetainsSession();
        UnlockAndCancellationReleaseDeferredScopes();

        Console.WriteLine("TRADE_QUEST_REGRESSION_PASSED: pre-lock drain, post-drain deferral, disconnect credential retention, multiple scopes, unlock and cancellation cleanup.");
    }

    private static void ProgressBeforeLockParticipatesInDrain()
    {
        TradeServerState.ResetForServerShutdown();

        using var progress = TradeServerState.DeferInventoryMutation(FirstClientId);

        Require(progress.TryBegin(), "Progress before a trade lock must be admitted synchronously.");
        Require(progress.TryBegin(), "Repeated admission must be idempotent.");

        var commit = StartCommit();

        Require(TradeServerState.HasActiveInventoryMutation(FirstClientId), "Trade must drain progress even when its quest semaphore has not been acquired yet.");
        Require(TradeServerState.HasPendingInventoryMutation(FirstClientId), "Accepted progress must retain its player session.");

        progress.Dispose();

        Require(!TradeServerState.HasActiveInventoryMutation(FirstClientId), "Settled progress must release the trade drain.");
        Require(!TradeServerState.HasPendingInventoryMutation(FirstClientId), "Repeated admission must not leak a pending scope.");
        Require(TradeServerState.Complete(commit.SessionId), "Trade should complete after the earlier write settles.");
    }

    private static void ProgressAfterDrainWaitsForCommitAndRetainsSession()
    {
        TradeServerState.ResetForServerShutdown();
        var commit = StartCommit();

        Require(!TradeServerState.HasActiveInventoryMutation(FirstClientId), "The commit starts with its earlier writes drained.");

        using var firstProgress = TradeServerState.DeferInventoryMutation(FirstClientId);
        using var secondProgress = TradeServerState.DeferInventoryMutation(FirstClientId);

        Require(!firstProgress.TryBegin() && !secondProgress.TryBegin(), "Late party kills must not start while the trade POST owns the reservation.");
        Require(!TradeServerState.HasActiveInventoryMutation(FirstClientId), "Deferred progress must not deadlock the commit drain.");
        Require(TradeServerState.HasPendingInventoryMutation(FirstClientId), "Deferred accepted progress must retain its player session.");

        TradeServerState.RemovePlayer(FirstClientId);

        Require(TradeServerState.IsCommitPending(commit.SessionId), "Disconnect must preserve accepted commits.");
        Require(!firstProgress.TryBegin(), "Disconnect must not admit progress ahead of the commit.");

        TradeServerState.Complete(commit.SessionId);

        Require(TradeServerState.HasPendingInventoryMutation(FirstClientId), "Revocation must still wait after commit completion and before deferred admission.");
        Require(firstProgress.TryBegin() && secondProgress.TryBegin(), "Deferred progress must resume after commit settlement, including after disconnect.");

        firstProgress.Dispose();

        Require(TradeServerState.HasActiveInventoryMutation(FirstClientId), "One completed write must not clear another admitted write.");

        secondProgress.Dispose();

        Require(!TradeServerState.HasPendingInventoryMutation(FirstClientId), "Revocation can proceed only after every accepted write settles.");
    }

    private static void UnlockAndCancellationReleaseDeferredScopes()
    {
        TradeServerState.ResetForServerShutdown();
        StartTrade();
        TradeServerState.SetLocked(FirstClientId, true);

        using var cancelledProgress = TradeServerState.DeferInventoryMutation(FirstClientId);
        using var resumedProgress = TradeServerState.DeferInventoryMutation(FirstClientId);

        Require(!cancelledProgress.TryBegin() && !resumedProgress.TryBegin(), "A negotiation lock also defers quest progress.");

        cancelledProgress.Dispose();
        cancelledProgress.Dispose();

        Require(TradeServerState.HasPendingInventoryMutation(FirstClientId), "Cancellation must release only its own deferred scope.");

        TradeServerState.SetLocked(FirstClientId, false);

        Require(resumedProgress.TryBegin(), "An unlocked negotiation must resume accepted progress.");

        resumedProgress.Dispose();

        Require(!TradeServerState.HasPendingInventoryMutation(FirstClientId), "Disposed deferred and active scopes must leave no counter behind.");
    }

    private static void StartTrade()
    {
        Require(TradeServerState.Invite(FirstClientId, SecondClientId) == TradeOperationStatusEnum.Applied, "Trade invitation failed.");
        Require(TradeServerState.Respond(SecondClientId, FirstClientId, true) == TradeOperationStatusEnum.Applied, "Trade acceptance failed.");
    }

    private static TradeServerState.TradeCommit StartCommit()
    {
        StartTrade();

        Require(TradeServerState.SetOffer(FirstClientId, new[]
        {
            new InventoryItemDto { Type = InventoryItemEnum.Currency, Count = 1 }
        }) == TradeOperationStatusEnum.Applied, "Trade offer failed.");

        TradeServerState.SetLocked(FirstClientId, true);
        TradeServerState.SetLocked(SecondClientId, true);
        TradeServerState.Confirm(FirstClientId);
        TradeServerState.Confirm(SecondClientId);

        Require(TradeServerState.TryGetCommit(FirstClientId, out var commit), "Trade did not enter committing state.");

        return commit;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
