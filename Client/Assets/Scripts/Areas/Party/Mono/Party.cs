using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Friends.Enums;
using Assets.Scripts.Areas.Friends.Models;
using Assets.Scripts.Areas.Party.Enums;
using Assets.Scripts.Areas.Party.Models;
using Assets.Scripts.Areas.Party.UI;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Areas.Party.Mono
{
    public class Party : NetworkBehaviour
    {
        private const double _mutationCooldownSeconds = 0.5d;

        private CancellationTokenSource _networkLifetimeCancellationTokenSource;
        private bool _mutationInProgress;
        private double _nextMutationAt = double.NegativeInfinity;

        public static Party Local { get; private set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            CancelNetworkLifetime();
            _networkLifetimeCancellationTokenSource?.Dispose();
            _networkLifetimeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _mutationInProgress = false;
            _nextMutationAt = double.NegativeInfinity;

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
                PartyServerState.RemovePlayer(OwnerClientId);
                SendStateToAll();
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
            if (IsOwner && IsSpawned && characterId > 0)
            {
                InviteServerRpc(characterId);
            }
        }

        public void Respond(int inviterCharacterId, bool accept)
        {
            if (IsOwner && IsSpawned && inviterCharacterId > 0)
            {
                RespondServerRpc(inviterCharacterId, accept);
            }
        }

        public void Leave()
        {
            if (IsOwner && IsSpawned)
            {
                LeaveServerRpc();
            }
        }

        public static void NotifyCharacterChanged(ulong clientId)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            if (!PartyServerState.TryGetParty(clientId, out var memberClientIds, out _))
            {
                return;
            }

            foreach (var memberClientId in memberClientIds.ToArray())
            {
                SendStateTo(memberClientId);
            }
        }

        private async UniTask BindUiAsync()
        {
            var cancellationToken = GetNetworkLifetimeCancellationToken();
            var cancelled = await UniTask.WaitUntil(() => PartyUI.Instance != null, cancellationToken: cancellationToken).SuppressCancellationThrow();

            if (cancelled || !IsOwner || !CanUseNetworkLifetime(cancellationToken))
            {
                return;
            }

            PartyUI.Instance.Bind(this);
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
            if (!TryBeginMutation())
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
                    UserManager.Instance.GetPlayerSessionId(OwnerClientId),
                    log: false,
                    cancellationToken: cancellationToken);

                if (!CanUseNetworkLifetime(cancellationToken))
                {
                    return;
                }

                if (!authorization.IsAllowed)
                {
                    var rejectedStatus = authorization.Status == FriendOperationStatusEnum.CharacterNotFound
                        ? PartyOperationStatusEnum.CharacterNotFound
                        : PartyOperationStatusEnum.FriendRequired;

                    PartyOperationClientRpc(PartyOperationTypeEnum.Invite, rejectedStatus, authorization.CharacterName, OwnerClientId.ToClientRpcParams());

                    return;
                }

                if (!TryGetClientId(authorization.CharacterId, out var targetClientId))
                {
                    PartyOperationClientRpc(PartyOperationTypeEnum.Invite, PartyOperationStatusEnum.TargetOffline, authorization.CharacterName,
                        OwnerClientId.ToClientRpcParams());

                    return;
                }

                var status = PartyServerState.Invite(OwnerClientId, targetClientId);

                PartyOperationClientRpc(PartyOperationTypeEnum.Invite, status, authorization.CharacterName, OwnerClientId.ToClientRpcParams());

                if (status != PartyOperationStatusEnum.Applied)
                {
                    return;
                }

                SendStateToAll();
                SendNotification(targetClientId, PartyNotificationTypeEnum.InvitationReceived, GetOwnerCharacterName());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Party invitation failed: {exception.Message}");

                if (CanUseNetworkLifetime(cancellationToken))
                {
                    PartyOperationClientRpc(PartyOperationTypeEnum.Invite, PartyOperationStatusEnum.RequestFailed, string.Empty,
                        OwnerClientId.ToClientRpcParams());
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
            if (!TryBeginMutation())
            {
                return;
            }

            try
            {
                if (!TryGetClientId(inviterCharacterId, out var inviterClientId))
                {
                    PartyOperationClientRpc(
                        accept ? PartyOperationTypeEnum.Accept : PartyOperationTypeEnum.Decline,
                        PartyOperationStatusEnum.InvitationNotFound,
                        string.Empty,
                        OwnerClientId.ToClientRpcParams());

                    return;
                }

                var operation = accept ? PartyOperationTypeEnum.Accept : PartyOperationTypeEnum.Decline;
                var inviterName = GetCharacterName(inviterClientId);
                var status = PartyServerState.Respond(OwnerClientId, inviterClientId, accept);

                PartyOperationClientRpc(operation, status, inviterName, OwnerClientId.ToClientRpcParams());

                if (status != PartyOperationStatusEnum.Applied)
                {
                    return;
                }

                SendStateToAll();
                SendNotification(
                    inviterClientId,
                    accept ? PartyNotificationTypeEnum.InvitationAccepted : PartyNotificationTypeEnum.InvitationDeclined,
                    GetOwnerCharacterName());
            }
            finally
            {
                EndMutation(GetNetworkLifetimeCancellationToken());
            }
        }

        [ServerRpc]
        private void LeaveServerRpc()
        {
            if (!TryBeginMutation())
            {
                return;
            }

            try
            {
                var status = PartyServerState.Leave(OwnerClientId);

                PartyOperationClientRpc(PartyOperationTypeEnum.Leave, status, string.Empty, OwnerClientId.ToClientRpcParams());

                if (status == PartyOperationStatusEnum.Applied)
                {
                    SendStateToAll();
                }
            }
            finally
            {
                EndMutation(GetNetworkLifetimeCancellationToken());
            }
        }

        [ClientRpc]
        private void ReceiveStateClientRpc(string payload, ClientRpcParams rpcParams = default)
        {
            var snapshot = JsonSerializer.Deserialize<PartySnapshotDto>(payload) ?? new PartySnapshotDto();

            PartyUI.Instance?.Present(snapshot);
        }

        [ClientRpc]
        private void PartyOperationClientRpc(
            PartyOperationTypeEnum operation,
            PartyOperationStatusEnum status,
            string characterName,
            ClientRpcParams rpcParams = default)
        {
            PartyUI.Instance?.ShowOperationStatus(operation, status, characterName);
        }

        [ClientRpc]
        private void PartyNotificationClientRpc(
            PartyNotificationTypeEnum notification,
            string characterName,
            ClientRpcParams rpcParams = default)
        {
            PartyUI.Instance?.ShowNotification(notification, characterName);
        }

        private bool TryBeginMutation()
        {
            var now = Time.realtimeSinceStartupAsDouble;

            if (_mutationInProgress || now < _nextMutationAt)
            {
                PartyOperationClientRpc(PartyOperationTypeEnum.Invite, PartyOperationStatusEnum.RequestFailed, string.Empty,
                    OwnerClientId.ToClientRpcParams());

                return false;
            }

            _mutationInProgress = true;
            _nextMutationAt = now + _mutationCooldownSeconds;

            return true;
        }

        private void EndMutation(CancellationToken cancellationToken)
        {
            if (IsCurrentNetworkLifetime(cancellationToken))
            {
                _mutationInProgress = false;
            }
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
            if (!TryGetPartyController(clientId, out var controller))
            {
                return;
            }

            var snapshot = CreateSnapshot(clientId);
            var payload = JsonSerializer.Serialize(snapshot);

            controller.ReceiveStateClientRpc(payload, clientId.ToClientRpcParams());
        }

        private static PartySnapshotDto CreateSnapshot(ulong clientId)
        {
            var members = new List<PartyMemberDto>();

            if (PartyServerState.TryGetParty(clientId, out var memberClientIds, out var leaderClientId))
            {
                foreach (var memberClientId in memberClientIds)
                {
                    if (!UserManager.Instance.Characters.TryGetValue(memberClientId, out var character))
                    {
                        continue;
                    }

                    character.Levels.TryGetValue(ExperienceTypeEnum.Main, out var level);

                    members.Add(new PartyMemberDto
                    {
                        CharacterId = character.Id,
                        CharacterName = character.Name,
                        Health = character.Health,
                        MaxHealth = character.MaxHealth,
                        Level = level,
                        IsLeader = memberClientId == leaderClientId
                    });
                }
            }

            var invitations = PartyServerState.GetInviters(clientId)
                .Select(x => UserManager.Instance.Characters.TryGetValue(x, out var character)
                    ? new PartyInvitationDto
                    {
                        CharacterId = character.Id,
                        CharacterName = character.Name
                    }
                    : null)
                .Where(x => x != null)
                .OrderBy(x => x.CharacterName)
                .ToArray();

            return new PartySnapshotDto
            {
                Members = members.ToArray(),
                Invitations = invitations
            };
        }

        private static void SendNotification(ulong clientId, PartyNotificationTypeEnum notification, string characterName)
        {
            if (TryGetPartyController(clientId, out var controller))
            {
                controller.PartyNotificationClientRpc(notification, characterName, clientId.ToClientRpcParams());
            }
        }

        private static bool TryGetPartyController(ulong clientId, out Party controller)
        {
            controller = null;

            return NetworkManager.Singleton != null
                && NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)
                && client.PlayerObject != null
                && client.PlayerObject.TryGetComponent(out controller)
                && controller.IsSpawned;
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

        private string GetOwnerCharacterName()
        {
            return GetCharacterName(OwnerClientId);
        }

        private static string GetCharacterName(ulong clientId)
        {
            return UserManager.Instance.Characters.TryGetValue(clientId, out var character) ? character.Name : string.Empty;
        }

        private CancellationToken GetNetworkLifetimeCancellationToken()
        {
            return _networkLifetimeCancellationTokenSource?.Token ?? new CancellationToken(canceled: true);
        }

        private bool IsCurrentNetworkLifetime(CancellationToken cancellationToken)
        {
            return _networkLifetimeCancellationTokenSource != null
                && _networkLifetimeCancellationTokenSource.Token == cancellationToken;
        }

        private bool CanUseNetworkLifetime(CancellationToken cancellationToken)
        {
            return IsCurrentNetworkLifetime(cancellationToken)
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

            PartyUI.Instance?.Unbind(this);
            Local = null;
        }
    }
}
