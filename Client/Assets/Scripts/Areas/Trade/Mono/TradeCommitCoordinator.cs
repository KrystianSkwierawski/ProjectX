using System;
using System.Threading;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Trade.Enums;
using Assets.Scripts.Areas.Trade.Models;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Areas.Trade.Mono
{
    public static class TradeCommitCoordinator
    {
        private const int _maximumCommitRetryDelaySeconds = 30;

        private static CancellationTokenSource _serverLifetimeCancellationTokenSource;
        private static NetworkManager _networkManager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            ReleaseServerLifetime(resetTradeState: false);
        }

        public static void Start(TradeServerState.TradeCommit commit)
        {
            if (!TryGetServerLifetimeToken(out var cancellationToken))
            {
                Trade.FailCommit(commit, TradeOperationStatusEnum.RequestFailed);

                return;
            }

            TradeCharacterInventoriesCommand command;
            string sourcePlayerSessionId;

            try
            {
                command = new TradeCharacterInventoriesCommand
                {
                    TradeId = commit.SessionId,
                    OtherPlayerSessionId = UserManager.Instance.GetPlayerSessionId(commit.SecondClientId),
                    Give = commit.FirstOffer,
                    Receive = commit.SecondOffer
                };
                sourcePlayerSessionId = UserManager.Instance.GetPlayerSessionId(commit.FirstClientId);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Trade commit could not start: {exception.Message}");
                Trade.FailCommit(commit, TradeOperationStatusEnum.RequestFailed);

                return;
            }

            CommitAsync(commit, command, sourcePlayerSessionId, cancellationToken)
                .SuppressCancellationThrow()
                .Forget();
        }

        internal static void BindServerLifetime()
        {
            TryGetServerLifetimeToken(out _);
        }

        internal static CancellationToken GetServerLifetimeCancellationToken()
        {
            return TryGetServerLifetimeToken(out var cancellationToken)
                ? cancellationToken
                : new CancellationToken(canceled: true);
        }

        private static async UniTask CommitAsync(
            TradeServerState.TradeCommit commit,
            TradeCharacterInventoriesCommand command,
            string sourcePlayerSessionId,
            CancellationToken cancellationToken)
        {
            var retryDelaySeconds = 1;
            var retryNotificationSent = false;
            var commitOutcomeUncertain = false;

            await UniTask.WaitUntil(
                () => !TradeServerState.IsCommitPending(commit.SessionId)
                    || (!TradeServerState.HasPendingInventoryMutation(commit.FirstClientId)
                        && !TradeServerState.HasPendingInventoryMutation(commit.SecondClientId)),
                cancellationToken: cancellationToken);

            if (!TradeServerState.IsCommitPending(commit.SessionId))
            {
                return;
            }

            while (TradeServerState.IsCommitPending(commit.SessionId))
            {
                cancellationToken.ThrowIfCancellationRequested();
                TradeCharacterInventoriesDto result;

                try
                {
                    result = await UnityWebRequestHelper.ExecutePostAsync<TradeCharacterInventoriesDto>(
                        "CharacterInventories/Trade",
                        command,
                        sourcePlayerSessionId,
                        log: false,
                        cancellationToken: cancellationToken);

                    if (result == null)
                    {
                        throw new InvalidOperationException("The trade API returned an empty response.");
                    }

                    if (result.Status == TradeCharacterInventoriesStatusEnum.Applied
                        && (!IsValidInventorySnapshot(result.SourceInventory)
                            || !IsValidInventorySnapshot(result.TargetInventory)))
                    {
                        throw new InvalidOperationException("The trade API returned an applied result without both inventory snapshots.");
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    if (!TradeServerState.IsCommitPending(commit.SessionId))
                    {
                        return;
                    }

                    var isDefinitiveFailure = IsDefinitiveCommitFailure(exception);

                    if (isDefinitiveFailure && !commitOutcomeUncertain)
                    {
                        Debug.LogWarning($"Trade commit was rejected and will not be retried: {exception.Message}");
                        Trade.FailCommit(commit, TradeOperationStatusEnum.RequestFailed);

                        return;
                    }

                    commitOutcomeUncertain = true;
                    Debug.LogWarning($"Trade commit result is uncertain; checking its durable receipt: {exception.Message}");
                    result = await TryResolveCommitAsync(commit.SessionId, cancellationToken);

                    if (result?.Status == TradeCharacterInventoriesStatusEnum.Applied)
                    {
                        Debug.Log("The durable trade receipt confirmed that the commit was applied.");
                    }
                    else
                    {
                        if (isDefinitiveFailure
                            && result?.Status == TradeCharacterInventoriesStatusEnum.ReceiptNotFound)
                        {
                            Debug.LogWarning("The retried trade commit was rejected and no durable receipt exists.");
                            Trade.FailCommit(commit, TradeOperationStatusEnum.RequestFailed);

                            return;
                        }

                        if (!retryNotificationSent)
                        {
                            Trade.SendOperation(
                                commit.FirstClientId,
                                TradeOperationTypeEnum.Confirm,
                                TradeOperationStatusEnum.CommitRetrying,
                                Trade.GetCharacterName(commit.SecondClientId));
                            Trade.SendOperation(
                                commit.SecondClientId,
                                TradeOperationTypeEnum.Confirm,
                                TradeOperationStatusEnum.CommitRetrying,
                                Trade.GetCharacterName(commit.FirstClientId));
                            retryNotificationSent = true;
                        }

                        await UniTask.Delay(
                            TimeSpan.FromSeconds(retryDelaySeconds),
                            ignoreTimeScale: true,
                            cancellationToken: cancellationToken);
                        retryDelaySeconds = Math.Min(retryDelaySeconds * 2, _maximumCommitRetryDelaySeconds);

                        continue;
                    }
                }

                if (result.Status != TradeCharacterInventoriesStatusEnum.Applied)
                {
                    Trade.FailCommit(commit, MapCommitFailure(result.Status));

                    return;
                }

                if (!TradeServerState.Complete(commit.SessionId))
                {
                    return;
                }

                var firstInventory = GetInventorySnapshotForClient(
                    commit.FirstClientId,
                    result.SourceInventory,
                    result.TargetInventory);
                var secondInventory = GetInventorySnapshotForClient(
                    commit.SecondClientId,
                    result.SourceInventory,
                    result.TargetInventory);

                Trade.CompleteCommittedTransfer(
                    commit.FirstClientId,
                    firstInventory,
                    commit.FirstOffer,
                    commit.SecondOffer,
                    Trade.GetCharacterName(commit.SecondClientId));
                Trade.CompleteCommittedTransfer(
                    commit.SecondClientId,
                    secondInventory,
                    commit.SecondOffer,
                    commit.FirstOffer,
                    Trade.GetCharacterName(commit.FirstClientId));

                return;
            }
        }

        private static bool TryGetServerLifetimeToken(out CancellationToken cancellationToken)
        {
            var networkManager = NetworkManager.Singleton;

            if (networkManager == null || !networkManager.IsServer || !networkManager.IsListening)
            {
                cancellationToken = new CancellationToken(canceled: true);

                return false;
            }

            if (_networkManager != networkManager || _serverLifetimeCancellationTokenSource == null)
            {
                ReleaseServerLifetime(resetTradeState: !ReferenceEquals(_networkManager, null));
                _networkManager = networkManager;
                _networkManager.OnServerStopped += HandleServerStopped;
                _serverLifetimeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
            }

            cancellationToken = _serverLifetimeCancellationTokenSource.Token;

            return !cancellationToken.IsCancellationRequested;
        }

        private static void HandleServerStopped(bool _)
        {
            ReleaseServerLifetime(resetTradeState: true);
        }

        private static void ReleaseServerLifetime(bool resetTradeState)
        {
            if (!ReferenceEquals(_networkManager, null))
            {
                _networkManager.OnServerStopped -= HandleServerStopped;
            }

            _networkManager = null;
            _serverLifetimeCancellationTokenSource?.Cancel();
            _serverLifetimeCancellationTokenSource?.Dispose();
            _serverLifetimeCancellationTokenSource = null;

            if (resetTradeState)
            {
                TradeServerState.ResetForServerShutdown();
            }
        }

        private static bool IsDefinitiveCommitFailure(Exception exception)
        {
            return exception is ApiRequestException apiException
                && apiException.ResponseCode >= 400
                && apiException.ResponseCode < 500
                && apiException.ResponseCode != 408
                && apiException.ResponseCode != 409
                && apiException.ResponseCode != 425
                && apiException.ResponseCode != 429;
        }

        private static async UniTask<TradeCharacterInventoriesDto> TryResolveCommitAsync(
            Guid tradeId,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await UnityWebRequestHelper.ExecutePostAsync<TradeCharacterInventoriesDto>(
                    "CharacterInventories/Trade/Resolve",
                    new ResolveCharacterInventoryTradeCommand { TradeId = tradeId },
                    log: false,
                    cancellationToken: cancellationToken);

                if (result?.Status == TradeCharacterInventoriesStatusEnum.Applied
                    && (!IsValidInventorySnapshot(result.SourceInventory)
                        || !IsValidInventorySnapshot(result.TargetInventory)))
                {
                    throw new InvalidOperationException("The trade receipt returned an applied result without both inventory snapshots.");
                }

                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"The durable trade receipt could not be resolved yet: {exception.Message}");

                return null;
            }
        }

        private static TradeOperationStatusEnum MapCommitFailure(TradeCharacterInventoriesStatusEnum status)
        {
            return status switch
            {
                TradeCharacterInventoriesStatusEnum.SourceInventoryFull => TradeOperationStatusEnum.InventoryFull,
                TradeCharacterInventoriesStatusEnum.TargetInventoryFull => TradeOperationStatusEnum.InventoryFull,
                TradeCharacterInventoriesStatusEnum.SourceItemsUnavailable => TradeOperationStatusEnum.ItemsUnavailable,
                TradeCharacterInventoriesStatusEnum.TargetItemsUnavailable => TradeOperationStatusEnum.ItemsUnavailable,
                TradeCharacterInventoriesStatusEnum.InventoryChanged => TradeOperationStatusEnum.InventoryChanged,
                _ => TradeOperationStatusEnum.RequestFailed
            };
        }

        private static bool IsValidInventorySnapshot(CharacterInventoryDto inventory)
        {
            return inventory?.Inventory?.Items != null
                && inventory.CharacterId > 0
                && inventory.Count >= inventory.Inventory.Items.Count;
        }

        private static CharacterInventoryDto GetInventorySnapshotForClient(
            ulong clientId,
            CharacterInventoryDto sourceInventory,
            CharacterInventoryDto targetInventory)
        {
            int characterId;

            try
            {
                characterId = UserManager.Instance.GetPlayerCharacterId(clientId);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Trade inventory snapshot could not be routed to client {clientId}: {exception.Message}");

                return null;
            }

            if (sourceInventory.CharacterId == characterId)
            {
                return sourceInventory;
            }

            if (targetInventory.CharacterId == characterId)
            {
                return targetInventory;
            }

            Debug.LogWarning($"The applied trade response does not contain inventory for character {characterId}. The client will reload it.");

            return null;
        }
    }
}
