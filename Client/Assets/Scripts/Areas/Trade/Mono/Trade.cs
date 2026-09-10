using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Friends.Enums;
using Assets.Scripts.Areas.Friends.Models;
using Assets.Scripts.Areas.Inventory;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.Mono;
using Assets.Scripts.Areas.Quest.Enums;
using Assets.Scripts.Areas.Quest.Subscriptions;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Trade.Enums;
using Assets.Scripts.Areas.Trade.Models;
using Assets.Scripts.Areas.Trade.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Areas.Trade.Mono
{
    public class Trade : NetworkBehaviour
    {
        private const double _mutationCooldownSeconds = 0.25d;

        private CancellationTokenSource _networkLifetimeCancellationTokenSource;
        private TradeSnapshotDto _snapshot = new TradeSnapshotDto();
        private bool _mutationInProgress;
        private bool _cancelRequested;
        private bool _serverCancellationPending;
        private bool _offerMutationPending;
        private bool _offerResponseReceived;
        private double _nextClientMutationAt = double.NegativeInfinity;
        private double _nextServerMutationAt = double.NegativeInfinity;

        public static Trade Local { get; private set; }

        public bool IsInventoryLocked => _snapshot.HasSession && (_snapshot.MyLocked || _snapshot.IsCommitting);

        public static void NotifyInventoryLocked(ulong clientId)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                SendOperation(clientId, TradeOperationTypeEnum.Offer, TradeOperationStatusEnum.OfferLocked, string.Empty);

                return;
            }

            ShowLocalStatus(TradeOperationTypeEnum.Offer, TradeOperationStatusEnum.OfferLocked);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            CancelNetworkLifetime();
            _networkLifetimeCancellationTokenSource?.Dispose();
            _networkLifetimeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _mutationInProgress = false;
            _cancelRequested = false;
            _serverCancellationPending = false;
            _offerMutationPending = false;
            _offerResponseReceived = false;
            _nextClientMutationAt = double.NegativeInfinity;
            _nextServerMutationAt = double.NegativeInfinity;

            if (IsServer)
            {
                TradeCommitCoordinator.BindServerLifetime();
            }

            if (!IsOwner)
            {
                return;
            }

            Local = this;
            BindUiAsync().Forget();
        }

        public override void OnNetworkDespawn()
        {
            CancelNetworkLifetime();

            if (IsServer)
            {
                var wasCommitting = TradeServerState.TryGetSession(OwnerClientId, out var session) && session.IsCommitting;
                var partnerClientId = TradeServerState.RemovePlayer(OwnerClientId);

                SendStateToAll();

                if (partnerClientId.HasValue && !wasCommitting)
                {
                    SendNotification(partnerClientId.Value, TradeNotificationTypeEnum.Cancelled, GetCharacterName(OwnerClientId));
                }
            }

            ClearLocalInstance();

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            CancelNetworkLifetime();
            _networkLifetimeCancellationTokenSource?.Dispose();
            _networkLifetimeCancellationTokenSource = null;
            ClearLocalInstance();

            base.OnDestroy();
        }

        public void Invite(int characterId)
        {
            if (CanSendMutation() && characterId > 0)
            {
                InviteServerRpc(characterId);
            }
        }

        public void Respond(int inviterCharacterId, bool accept)
        {
            if (CanSendMutation() && inviterCharacterId > 0)
            {
                RespondServerRpc(inviterCharacterId, accept);
            }
        }

        public bool IsOfferChangePending => _offerMutationPending;

        public void AddItemToOffer(InventoryItemDto item, int sourceSlotIndex)
        {
            if (!CanSendMutation()
                || !_snapshot.HasSession
                || _snapshot.MyLocked
                || item == null
                || item.Type == InventoryItemEnum.None
                || item.Count <= 0)
            {
                return;
            }

            var offer = CloneItems(_snapshot.MyOffer).ToList();
            var existing = offer.FirstOrDefault(x => x.Type == item.Type);
            var offeredCount = existing?.Count ?? 0;
            var availableCount = InventoryManager.Instance.Dto.Inventory.Items
                .Where(x => x.Type == item.Type)
                .Sum(x => x.Count);

            if ((long)offeredCount + item.Count > availableCount)
            {
                ShowLocalStatus(TradeOperationTypeEnum.Offer, TradeOperationStatusEnum.ItemsUnavailable);

                return;
            }

            if (existing == null)
            {
                offer.Add(CloneItem(item));
            }
            else
            {
                existing.Count += item.Count;
            }

            _offerMutationPending = true;
            TradeUI.Instance?.PreviewOfferAddition(offer.ToArray(), sourceSlotIndex, item);
            SetOfferServerRpc(offer.ToArray());
        }

        public void RemoveItemFromOffer(InventoryItemEnum type)
        {
            if (!CanSendMutation() || !_snapshot.HasSession || _snapshot.MyLocked)
            {
                return;
            }

            var offer = CloneItems(_snapshot.MyOffer)
                .Where(x => x.Type != type)
                .ToArray();

            _offerMutationPending = true;
            SetOfferServerRpc(offer);
        }

        public void ToggleLock()
        {
            if (CanSendMutation() && _snapshot.HasSession && !_snapshot.IsCommitting)
            {
                SetLockedServerRpc(!_snapshot.MyLocked);
            }
        }

        public void Confirm()
        {
            if (CanSendMutation() && _snapshot.HasSession && !_snapshot.IsCommitting)
            {
                ConfirmServerRpc();
            }
        }

        public void Cancel()
        {
            if (IsOwner && IsSpawned && !_cancelRequested && !_snapshot.IsCommitting)
            {
                _cancelRequested = true;
                CancelServerRpc();
            }
        }

        private bool CanSendMutation()
        {
            if (!IsOwner || !IsSpawned || _cancelRequested || _offerMutationPending || Time.realtimeSinceStartupAsDouble < _nextClientMutationAt)
            {
                return false;
            }

            _nextClientMutationAt = Time.realtimeSinceStartupAsDouble + _mutationCooldownSeconds;

            return true;
        }

        private async UniTask BindUiAsync()
        {
            var cancellationToken = GetNetworkLifetimeCancellationToken();
            var cancelled = await UniTask.WaitUntil(() => TradeUI.Instance != null, cancellationToken: cancellationToken).SuppressCancellationThrow();

            if (cancelled || !IsOwner || !CanUseNetworkLifetime(cancellationToken))
            {
                return;
            }

            TradeUI.Instance.Bind(this);
            RequestStateServerRpc();
        }

        [ServerRpc]
        private void RequestStateServerRpc()
        {
            SendStateTo(OwnerClientId);
        }

        [ServerRpc]
        private void InviteServerRpc(int characterId)
        {
            if (!TryBeginMutation(TradeOperationTypeEnum.Invite))
            {
                return;
            }

            InviteAsync(characterId).Forget();
        }

        private async UniTask InviteAsync(int characterId)
        {
            var cancellationToken = GetNetworkLifetimeCancellationToken();

            try
            {
                var authorization = await UnityWebRequestHelper.ExecuteGetAsync<AuthorizeWhisperDto>(
                    $"Friends/{characterId}/WhisperAuthorization",
                    GetPlayerSessionId(OwnerClientId),
                    log: false,
                    cancellationToken: cancellationToken);

                if (!CanUseNetworkLifetime(cancellationToken))
                {
                    return;
                }

                if (!authorization.IsAllowed)
                {
                    var rejectedStatus = authorization.Status == FriendOperationStatusEnum.CharacterNotFound
                        ? TradeOperationStatusEnum.TargetOffline
                        : TradeOperationStatusEnum.FriendRequired;

                    SendOperation(OwnerClientId, TradeOperationTypeEnum.Invite, rejectedStatus, authorization.CharacterName);

                    return;
                }

                if (!TryGetClientId(authorization.CharacterId, out var targetClientId))
                {
                    SendOperation(OwnerClientId, TradeOperationTypeEnum.Invite, TradeOperationStatusEnum.TargetOffline, authorization.CharacterName);

                    return;
                }

                var status = TradeServerState.Invite(OwnerClientId, targetClientId);

                SendOperation(OwnerClientId, TradeOperationTypeEnum.Invite, status, authorization.CharacterName);

                if (status != TradeOperationStatusEnum.Applied)
                {
                    return;
                }

                SendStateTo(OwnerClientId);
                SendStateTo(targetClientId);
                SendNotification(targetClientId, TradeNotificationTypeEnum.InvitationReceived, GetCharacterName(OwnerClientId));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Trade invitation failed: {exception.Message}");

                if (CanUseNetworkLifetime(cancellationToken))
                {
                    SendOperation(OwnerClientId, TradeOperationTypeEnum.Invite, TradeOperationStatusEnum.RequestFailed, string.Empty);
                }
            }
            finally
            {
                EndMutation(cancellationToken);
            }
        }

        [ServerRpc]
        private void RespondServerRpc(int inviterCharacterId, bool accept)
        {
            var operation = accept ? TradeOperationTypeEnum.Accept : TradeOperationTypeEnum.Decline;

            if (!TryBeginMutation(operation))
            {
                return;
            }

            try
            {
                if (!TryGetClientId(inviterCharacterId, out var inviterClientId))
                {
                    SendOperation(OwnerClientId, operation, TradeOperationStatusEnum.InvitationNotFound, string.Empty);

                    return;
                }

                var status = TradeServerState.Respond(OwnerClientId, inviterClientId, accept);

                SendOperation(OwnerClientId, operation, status, GetCharacterName(inviterClientId));

                if (status != TradeOperationStatusEnum.Applied)
                {
                    return;
                }

                SendStateTo(OwnerClientId);
                SendStateTo(inviterClientId);
                SendNotification(
                    inviterClientId,
                    accept ? TradeNotificationTypeEnum.InvitationAccepted : TradeNotificationTypeEnum.InvitationDeclined,
                    GetCharacterName(OwnerClientId));
            }
            finally
            {
                EndMutation(GetNetworkLifetimeCancellationToken());
            }
        }

        [ServerRpc]
        private void SetOfferServerRpc(InventoryItemDto[] offer)
        {
            if (!TryBeginMutation(TradeOperationTypeEnum.Offer))
            {
                return;
            }

            try
            {
                var status = TradeServerState.SetOffer(OwnerClientId, offer);

                SendOperation(OwnerClientId, TradeOperationTypeEnum.Offer, status, string.Empty);
                SendSessionState(OwnerClientId);
            }
            finally
            {
                EndMutation(GetNetworkLifetimeCancellationToken());
            }
        }

        [ServerRpc]
        private void SetLockedServerRpc(bool locked)
        {
            var operation = locked ? TradeOperationTypeEnum.Lock : TradeOperationTypeEnum.Unlock;

            if (!TryBeginMutation(operation))
            {
                return;
            }

            try
            {
                var status = TradeServerState.SetLocked(OwnerClientId, locked);

                SendOperation(OwnerClientId, operation, status, string.Empty);
                SendSessionState(OwnerClientId);
            }
            finally
            {
                EndMutation(GetNetworkLifetimeCancellationToken());
            }
        }

        [ServerRpc]
        private void ConfirmServerRpc()
        {
            if (!TryBeginMutation(TradeOperationTypeEnum.Confirm))
            {
                return;
            }

            try
            {
                var status = TradeServerState.Confirm(OwnerClientId);

                SendOperation(OwnerClientId, TradeOperationTypeEnum.Confirm, status, string.Empty);
                SendSessionState(OwnerClientId);

                if (status == TradeOperationStatusEnum.Applied && TradeServerState.TryGetCommit(OwnerClientId, out var commit))
                {
                    TradeCommitCoordinator.Start(commit);
                }
            }
            finally
            {
                EndMutation(GetNetworkLifetimeCancellationToken());
            }
        }

        [ServerRpc]
        private void CancelServerRpc()
        {
            if (_serverCancellationPending)
            {
                return;
            }

            _serverCancellationPending = true;
            CancelWhenReadyAsync().Forget();
        }

        private async UniTask CancelWhenReadyAsync()
        {
            var cancellationToken = GetNetworkLifetimeCancellationToken();

            try
            {
                await UniTask.WaitUntil(
                    () => !_mutationInProgress && Time.realtimeSinceStartupAsDouble >= _nextServerMutationAt,
                    cancellationToken: cancellationToken);

                if (!CanUseNetworkLifetime(cancellationToken))
                {
                    return;
                }

                _mutationInProgress = true;
                _nextServerMutationAt = Time.realtimeSinceStartupAsDouble + _mutationCooldownSeconds;
                var status = TradeServerState.Cancel(OwnerClientId, out var partnerClientId);

                SendOperation(OwnerClientId, TradeOperationTypeEnum.Cancel, status, string.Empty);
                SendStateTo(OwnerClientId);

                if (status == TradeOperationStatusEnum.Applied && partnerClientId != default)
                {
                    SendStateTo(partnerClientId);
                    SendNotification(partnerClientId, TradeNotificationTypeEnum.Cancelled, GetCharacterName(OwnerClientId));
                }

                SendStateToAll();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                if (CanUseNetworkLifetime(cancellationToken))
                {
                    _serverCancellationPending = false;
                    EndMutation(cancellationToken);
                }
            }
        }

        internal static void FailCommit(TradeServerState.TradeCommit commit, TradeOperationStatusEnum status)
        {
            if (!TradeServerState.FailCommit(commit.SessionId))
            {
                return;
            }

            if (!IsClientConnected(commit.FirstClientId) || !IsClientConnected(commit.SecondClientId))
            {
                TradeServerState.Complete(commit.SessionId);
            }

            SendStateTo(commit.FirstClientId);
            SendStateTo(commit.SecondClientId);
            SendOperation(commit.FirstClientId, TradeOperationTypeEnum.Confirm, status, GetCharacterName(commit.SecondClientId));
            SendOperation(commit.SecondClientId, TradeOperationTypeEnum.Confirm, status, GetCharacterName(commit.FirstClientId));
        }

        internal static void CompleteCommittedTransfer(
            ulong clientId,
            CharacterInventoryDto inventory,
            InventoryItemDto[] remove,
            InventoryItemDto[] add,
            string partnerName)
        {
            if (TryGetController(clientId, out var controller))
            {
                controller.CompleteTradeClientRpc(
                    JsonSerializer.Serialize(inventory),
                    partnerName,
                    clientId.ToClientRpcParams());
            }

            // The API commits persistent Collect progress with the inventories. This only refreshes
            // the runtime quest view for a participant that is still connected.
            if (!UserManager.Instance.TryGetPlayerSessionId(clientId, out var playerSessionId))
            {
                return;
            }

            foreach (var itemType in remove
                .Concat(add)
                .Select(x => x.Type)
                .Distinct())
            {
                CheckCharacterQuestSubscription.Instance.Invoke(clientId.ToString(), new CheckCharacterQuestSubscriptionEvent
                {
                    Progress = 0,
                    QuestType = QuestTypeEnum.Collect,
                    GameObjectName = itemType.ToString(),
                    PlayerSessionId = playerSessionId
                });
            }
        }

        private static void SendSessionState(ulong clientId)
        {
            if (!TradeServerState.TryGetSession(clientId, out var session))
            {
                SendStateTo(clientId);

                return;
            }

            SendStateTo(session.FirstClientId);
            SendStateTo(session.SecondClientId);
        }

        private static void SendStateToAll()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                return;
            }

            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds.ToArray())
            {
                SendStateTo(clientId);
            }
        }

        private static void SendStateTo(ulong clientId)
        {
            if (!TryGetController(clientId, out var controller))
            {
                return;
            }

            var payload = JsonSerializer.Serialize(CreateSnapshot(clientId));

            controller.ReceiveStateClientRpc(payload, clientId.ToClientRpcParams());
        }

        private static TradeSnapshotDto CreateSnapshot(ulong clientId)
        {
            var snapshot = new TradeSnapshotDto();

            if (TradeServerState.TryGetInvitation(clientId, out var inviterClientId))
            {
                snapshot.Invitation = CreateInvitation(inviterClientId);
            }

            if (TradeServerState.TryGetOutgoingInvitation(clientId, out var targetClientId))
            {
                snapshot.OutgoingInvitation = CreateInvitation(targetClientId);
            }

            if (!TradeServerState.TryGetSession(clientId, out var session))
            {
                return snapshot;
            }

            var partnerClientId = session.GetPartner(clientId);

            snapshot.HasSession = true;
            snapshot.PartnerName = GetCharacterName(partnerClientId);
            snapshot.MyOffer = CloneItems(session.GetOffer(clientId));
            snapshot.PartnerOffer = CloneItems(session.GetOffer(partnerClientId));
            snapshot.MyLocked = session.IsLocked(clientId);
            snapshot.PartnerLocked = session.IsLocked(partnerClientId);
            snapshot.MyConfirmed = session.IsConfirmed(clientId);
            snapshot.PartnerConfirmed = session.IsConfirmed(partnerClientId);
            snapshot.IsCommitting = session.IsCommitting;

            return snapshot;
        }

        private static TradeInvitationDto CreateInvitation(ulong clientId)
        {
            return UserManager.Instance.Characters.TryGetValue(clientId, out var character)
                ? new TradeInvitationDto
                {
                    CharacterId = character.Id,
                    CharacterName = character.Name
                }
                : null;
        }

        internal static void SendOperation(
            ulong clientId,
            TradeOperationTypeEnum operation,
            TradeOperationStatusEnum status,
            string characterName)
        {
            if (TryGetController(clientId, out var controller))
            {
                controller.OperationClientRpc(operation, status, characterName, clientId.ToClientRpcParams());
            }
        }

        private static void SendNotification(ulong clientId, TradeNotificationTypeEnum notification, string characterName)
        {
            if (TryGetController(clientId, out var controller))
            {
                controller.NotificationClientRpc(notification, characterName, clientId.ToClientRpcParams());
            }
        }

        [ClientRpc]
        private void ReceiveStateClientRpc(string payload, ClientRpcParams rpcParams = default)
        {
            _snapshot = JsonSerializer.Deserialize<TradeSnapshotDto>(payload) ?? new TradeSnapshotDto();

            if (_offerResponseReceived || !_snapshot.HasSession)
            {
                CompleteOfferChange();
            }

            if (!_snapshot.HasSession && _snapshot.Invitation == null && _snapshot.OutgoingInvitation == null)
            {
                _cancelRequested = false;
            }

            TradeUI.Instance?.Present(_snapshot);
        }

        [ClientRpc]
        private void CompleteTradeClientRpc(
            string inventoryPayload,
            string partnerName,
            ClientRpcParams rpcParams = default)
        {
            var inventory = JsonSerializer.Deserialize<CharacterInventoryDto>(inventoryPayload);
            // Trade is on the player root; CharacterInventory is on its character child.
            var characterInventory = GetComponentInChildren<CharacterInventory>(includeInactive: true);

            _snapshot = new TradeSnapshotDto();
            _cancelRequested = false;
            CompleteOfferChange();

            TradeUI.Instance?.Present(_snapshot);
            if (characterInventory == null)
            {
                Debug.LogError("Trade completed, but the player hierarchy has no CharacterInventory component.", this);

                return;
            }

            characterInventory.ApplyAuthoritativeInventory(inventory);
            characterInventory.ReloadAuthoritativeInventory();
            TradeUI.Instance?.ShowNotification(TradeNotificationTypeEnum.Completed, partnerName);
        }

        [ClientRpc]
        private void OperationClientRpc(
            TradeOperationTypeEnum operation,
            TradeOperationStatusEnum status,
            string characterName,
            ClientRpcParams rpcParams = default)
        {
            if (operation == TradeOperationTypeEnum.Offer && _offerMutationPending)
            {
                if (status == TradeOperationStatusEnum.Applied)
                {
                    // The matching authoritative snapshot follows this acknowledgement.
                    _offerResponseReceived = true;
                }
                else
                {
                    CompleteOfferChange();
                    TradeUI.Instance?.Present(_snapshot);
                }
            }

            if (operation == TradeOperationTypeEnum.Cancel)
            {
                _cancelRequested = false;
            }

            ShowLocalStatus(operation, status, characterName);
        }

        private void CompleteOfferChange()
        {
            _offerMutationPending = false;
            _offerResponseReceived = false;
            TradeUI.Instance?.ClearPendingOffer();
        }

        [ClientRpc]
        private void NotificationClientRpc(
            TradeNotificationTypeEnum notification,
            string characterName,
            ClientRpcParams rpcParams = default)
        {
            TradeUI.Instance?.ShowNotification(notification, characterName);
        }

        private static void ShowLocalStatus(
            TradeOperationTypeEnum operation,
            TradeOperationStatusEnum status,
            string characterName = "")
        {
            TradeUI.Instance?.ShowOperationStatus(operation, status, characterName);
        }

        private static bool TryGetController(ulong clientId, out Trade controller)
        {
            controller = null;

            if (NetworkManager.Singleton == null
                || !NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)
                || client.PlayerObject == null)
            {
                return false;
            }

            controller = client.PlayerObject.GetComponent<Trade>();

            return controller != null;
        }

        private static bool IsClientConnected(ulong clientId)
        {
            return NetworkManager.Singleton != null
                && NetworkManager.Singleton.IsListening
                && NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId);
        }

        private static bool TryGetClientId(int characterId, out ulong clientId)
        {
            var character = UserManager.Instance.Characters.FirstOrDefault(x => x.Value.Id == characterId);

            if (character.Value == null)
            {
                clientId = default;

                return false;
            }

            clientId = character.Key;

            return true;
        }

        internal static string GetCharacterName(ulong clientId)
        {
            return UserManager.Instance.Characters.TryGetValue(clientId, out var character) ? character.Name : string.Empty;
        }

        private static string GetPlayerSessionId(ulong clientId)
        {
            return UserManager.Instance.GetPlayerSessionId(clientId);
        }

        private static InventoryItemDto[] CloneItems(IEnumerable<InventoryItemDto> items)
        {
            return items?.Select(CloneItem).ToArray() ?? Array.Empty<InventoryItemDto>();
        }

        private static InventoryItemDto CloneItem(InventoryItemDto item)
        {
            return new InventoryItemDto { Type = item.Type, Count = item.Count };
        }

        private CancellationToken GetNetworkLifetimeCancellationToken()
        {
            return _networkLifetimeCancellationTokenSource?.Token ?? new CancellationToken(canceled: true);
        }

        private bool TryBeginMutation(TradeOperationTypeEnum operation)
        {
            var now = Time.realtimeSinceStartupAsDouble;

            if (_mutationInProgress || _serverCancellationPending || now < _nextServerMutationAt)
            {
                SendOperation(OwnerClientId, operation, TradeOperationStatusEnum.RequestFailed, string.Empty);

                return false;
            }

            _mutationInProgress = true;
            _nextServerMutationAt = now + _mutationCooldownSeconds;

            return true;
        }

        private void EndMutation(CancellationToken cancellationToken)
        {
            if (_networkLifetimeCancellationTokenSource != null
                && _networkLifetimeCancellationTokenSource.Token == cancellationToken)
            {
                _mutationInProgress = false;
            }
        }

        private bool CanUseNetworkLifetime(CancellationToken cancellationToken)
        {
            return _networkLifetimeCancellationTokenSource != null
                && _networkLifetimeCancellationTokenSource.Token == cancellationToken
                && !cancellationToken.IsCancellationRequested
                && IsSpawned;
        }

        private void CancelNetworkLifetime()
        {
            if (_networkLifetimeCancellationTokenSource?.IsCancellationRequested == false)
            {
                _networkLifetimeCancellationTokenSource.Cancel();
            }
        }

        private void ClearLocalInstance()
        {
            if (Local != this)
            {
                return;
            }

            TradeUI.Instance?.Unbind(this);
            Local = null;
        }
    }
}
