using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Assets.Scripts.Areas.Character;
using Assets.Scripts.Areas.Friends.Enums;
using Assets.Scripts.Areas.Friends.Models;
using Assets.Scripts.Areas.Friends.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Friends.Mono
{
    public class FriendList : NetworkBehaviour
    {
        private const int _maximumWhisperLength = 200;
        private const double _friendMutationCooldownSeconds = 0.5d;
        private const double _refreshCooldownSeconds = 0.5d;
        private const double _whisperCooldownSeconds = 0.5d;

        private FriendListDto _friendList = new FriendListDto();
        private double _lastWhisperAt = double.NegativeInfinity;
        private double _nextFriendMutationAt = double.NegativeInfinity;
        private double _nextRefreshAt = double.NegativeInfinity;
        private bool _friendMutationInProgress;
        private bool _refreshInProgress;
        private bool _refreshRequested;
        private CancellationTokenSource _networkLifetimeCancellationTokenSource;

        public static FriendList Local { get; private set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            CancelNetworkLifetime();
            _networkLifetimeCancellationTokenSource?.Dispose();
            _networkLifetimeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _friendMutationInProgress = false;
            _refreshInProgress = false;
            _refreshRequested = false;
            _nextFriendMutationAt = double.NegativeInfinity;
            _nextRefreshAt = double.NegativeInfinity;

            if (!IsOwner)
            {
                return;
            }

            Local = this;
            BindUiAsync().Forget();
        }

        private void Update()
        {
            if (IsServer && _refreshRequested && !_refreshInProgress && Time.realtimeSinceStartupAsDouble >= _nextRefreshAt)
            {
                ProcessRefreshAsync().Forget();
            }

            if (!IsOwner || FriendListUI.Instance == null)
            {
                return;
            }

            if (Keyboard.current.oKey.wasPressedThisFrame && !InputFocusUI.IsAnyInputFocused)
            {
                FriendListUI.Instance.Toggle();
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame && FriendListUI.Instance.IsOpen)
            {
                FriendListUI.Instance.Hide();
            }
        }

        public override void OnNetworkDespawn()
        {
            CancelNetworkLifetime();
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

        public void RequestRefresh()
        {
            if (IsOwner && IsSpawned)
            {
                RefreshServerRpc();
            }
        }

        public void Invite(string characterName)
        {
            if (IsOwner && IsSpawned && !string.IsNullOrWhiteSpace(characterName))
            {
                SendInvitationServerRpc(characterName.Trim());
            }
        }

        public void Respond(int characterId, bool accept)
        {
            if (IsOwner && IsSpawned && characterId > 0)
            {
                RespondInvitationServerRpc(characterId, accept);
            }
        }

        public void Remove(int characterId)
        {
            if (IsOwner && IsSpawned && characterId > 0)
            {
                RemoveFriendServerRpc(characterId);
            }
        }

        public bool TrySendWhisperCommand(string command)
        {
            var value = command?.TrimStart();

            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("/w ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!TryParseWhisperCommand(value, out var characterName, out var message))
            {
                FriendListUI.Instance.ShowWhisperStatus(WhisperDeliveryStatusEnum.InvalidMessage);

                return true;
            }

            var friend = _friendList.Friends
                .FirstOrDefault(x => string.Equals(x.CharacterName, characterName, StringComparison.OrdinalIgnoreCase));

            if (friend == null)
            {
                FriendListUI.Instance.ShowOperationStatus(FriendOperationTypeEnum.Invite, FriendOperationStatusEnum.WhisperNotAllowed, characterName);

                return true;
            }

            if (string.IsNullOrWhiteSpace(message) || message.Length > _maximumWhisperLength)
            {
                FriendListUI.Instance.ShowWhisperStatus(WhisperDeliveryStatusEnum.InvalidMessage);

                return true;
            }

            SendWhisperServerRpc(friend.CharacterId, message);

            return true;
        }

        private static bool TryParseWhisperCommand(string command, out string characterName, out string message)
        {
            characterName = string.Empty;
            message = string.Empty;

            var value = command?.TrimStart();

            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("/w ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var recipientAndMessage = value.Substring(3).TrimStart();

            if (string.IsNullOrWhiteSpace(recipientAndMessage))
            {
                return false;
            }

            if (recipientAndMessage[0] == '"')
            {
                return TryParseQuotedWhisper(recipientAndMessage, out characterName, out message);
            }

            var separatorIndex = recipientAndMessage.IndexOfAny(new[] { ' ', '\t', '\r', '\n' });

            if (separatorIndex < 0)
            {
                characterName = recipientAndMessage;

                return true;
            }

            characterName = recipientAndMessage.Substring(0, separatorIndex);
            message = recipientAndMessage.Substring(separatorIndex + 1).Trim();

            return !string.IsNullOrWhiteSpace(characterName);
        }

        private static bool TryParseQuotedWhisper(string recipientAndMessage, out string characterName, out string message)
        {
            var characterNameBuilder = new StringBuilder();
            var escaped = false;

            characterName = string.Empty;
            message = string.Empty;

            for (var index = 1; index < recipientAndMessage.Length; index++)
            {
                var character = recipientAndMessage[index];

                if (escaped)
                {
                    characterNameBuilder.Append(character);
                    escaped = false;

                    continue;
                }

                if (character == '\\')
                {
                    escaped = true;

                    continue;
                }

                if (character != '"')
                {
                    characterNameBuilder.Append(character);

                    continue;
                }

                if (index + 1 < recipientAndMessage.Length && !char.IsWhiteSpace(recipientAndMessage[index + 1]))
                {
                    return false;
                }

                characterName = characterNameBuilder.ToString();
                message = recipientAndMessage.Substring(index + 1).Trim();

                return !string.IsNullOrWhiteSpace(characterName);
            }

            return false;
        }

        private async UniTask BindUiAsync()
        {
            var cancellationToken = GetNetworkLifetimeCancellationToken();
            var cancelled = await UniTask.WaitUntil(() => FriendListUI.Instance != null, cancellationToken: cancellationToken).SuppressCancellationThrow();

            if (cancelled || !IsOwner || !CanUseNetworkLifetime(cancellationToken))
            {
                return;
            }

            FriendListUI.Instance.Bind(this);
            RequestRefresh();
        }

        [ServerRpc]
        private void RefreshServerRpc()
        {
            QueueRefresh();
        }

        private void QueueRefresh()
        {
            _refreshRequested = true;
        }

        private async UniTask ProcessRefreshAsync()
        {
            var cancellationToken = GetNetworkLifetimeCancellationToken();

            _refreshInProgress = true;
            _refreshRequested = false;

            try
            {
                var result = await UnityWebRequestHelper.ExecuteGetAsync<FriendListDto>("Friends", GetPlayerSessionId(), log: false, cancellationToken: cancellationToken);
                var payload = JsonSerializer.Serialize(result ?? new FriendListDto());

                if (!_refreshRequested && CanUseNetworkLifetime(cancellationToken))
                {
                    ReceiveFriendListClientRpc(payload, OwnerClientId.ToClientRpcParams());
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Friend list refresh failed: {exception.Message}");

                if (!_refreshRequested && CanUseNetworkLifetime(cancellationToken))
                {
                    RequestFailedClientRpc(OwnerClientId.ToClientRpcParams());
                }
            }
            finally
            {
                if (IsCurrentNetworkLifetime(cancellationToken))
                {
                    _nextRefreshAt = Time.realtimeSinceStartupAsDouble + _refreshCooldownSeconds;
                    _refreshInProgress = false;
                }
            }
        }

        [ServerRpc]
        private void SendInvitationServerRpc(string characterName)
        {
            if (!TryBeginFriendMutation())
            {
                return;
            }

            SendInvitationAsync(characterName).Forget();
        }

        private async UniTask SendInvitationAsync(string characterName)
        {
            var cancellationToken = GetNetworkLifetimeCancellationToken();

            try
            {
                var result = await UnityWebRequestHelper.ExecutePostAsync<FriendOperationDto>("Friends/Invitations", new SendFriendInvitationCommand
                {
                    CharacterName = characterName
                }, GetPlayerSessionId(), log: false, cancellationToken: cancellationToken);

                if (!CanUseNetworkLifetime(cancellationToken))
                {
                    return;
                }

                FriendOperationClientRpc(FriendOperationTypeEnum.Invite, result.Status, result.CharacterName, OwnerClientId.ToClientRpcParams());

                if (result.Status == FriendOperationStatusEnum.Applied)
                {
                    QueueRefresh();

                    if (TryGetClientId(result.CharacterId, out var targetClientId))
                    {
                        FriendNotificationClientRpc(FriendNotificationTypeEnum.InvitationReceived, GetOwnerCharacterName(), targetClientId.ToClientRpcParams());
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Friend invitation failed: {exception.Message}");

                if (CanUseNetworkLifetime(cancellationToken))
                {
                    RequestFailedClientRpc(OwnerClientId.ToClientRpcParams());
                }
            }
            finally
            {
                EndFriendMutation(cancellationToken);
            }
        }

        [ServerRpc]
        private void RespondInvitationServerRpc(int characterId, bool accept)
        {
            if (!TryBeginFriendMutation())
            {
                return;
            }

            RespondInvitationAsync(characterId, accept).Forget();
        }

        private async UniTask RespondInvitationAsync(int characterId, bool accept)
        {
            var cancellationToken = GetNetworkLifetimeCancellationToken();

            try
            {
                var result = await UnityWebRequestHelper.ExecutePostAsync<FriendOperationDto>("Friends/Invitations/Respond", new RespondFriendInvitationCommand
                {
                    CharacterId = characterId,
                    Accept = accept
                }, GetPlayerSessionId(), log: false, cancellationToken: cancellationToken);

                if (!CanUseNetworkLifetime(cancellationToken))
                {
                    return;
                }

                var operation = accept ? FriendOperationTypeEnum.Accept : FriendOperationTypeEnum.Decline;

                FriendOperationClientRpc(operation, result.Status, result.CharacterName, OwnerClientId.ToClientRpcParams());

                if (result.Status == FriendOperationStatusEnum.Applied)
                {
                    QueueRefresh();

                    if (TryGetClientId(result.CharacterId, out var targetClientId))
                    {
                        var notification = accept ? FriendNotificationTypeEnum.InvitationAccepted : FriendNotificationTypeEnum.InvitationDeclined;
                        FriendNotificationClientRpc(notification, GetOwnerCharacterName(), targetClientId.ToClientRpcParams());
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Friend invitation response failed: {exception.Message}");

                if (CanUseNetworkLifetime(cancellationToken))
                {
                    RequestFailedClientRpc(OwnerClientId.ToClientRpcParams());
                }
            }
            finally
            {
                EndFriendMutation(cancellationToken);
            }
        }

        [ServerRpc]
        private void RemoveFriendServerRpc(int characterId)
        {
            if (!TryBeginFriendMutation())
            {
                return;
            }

            RemoveFriendAsync(characterId).Forget();
        }

        private async UniTask RemoveFriendAsync(int characterId)
        {
            var cancellationToken = GetNetworkLifetimeCancellationToken();

            try
            {
                var result = await UnityWebRequestHelper.ExecuteDeleteAsync<FriendOperationDto>(
                    $"Friends/{characterId}",
                    GetPlayerSessionId(),
                    cancellationToken: cancellationToken);

                if (!CanUseNetworkLifetime(cancellationToken))
                {
                    return;
                }

                FriendOperationClientRpc(FriendOperationTypeEnum.Remove, result.Status, result.CharacterName, OwnerClientId.ToClientRpcParams());

                if (result.Status == FriendOperationStatusEnum.Applied)
                {
                    QueueRefresh();

                    if (TryGetClientId(result.CharacterId, out var targetClientId))
                    {
                        FriendNotificationClientRpc(FriendNotificationTypeEnum.FriendRemoved, GetOwnerCharacterName(), targetClientId.ToClientRpcParams());
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Friend removal failed: {exception.Message}");

                if (CanUseNetworkLifetime(cancellationToken))
                {
                    RequestFailedClientRpc(OwnerClientId.ToClientRpcParams());
                }
            }
            finally
            {
                EndFriendMutation(cancellationToken);
            }
        }

        private bool TryBeginFriendMutation()
        {
            var now = Time.realtimeSinceStartupAsDouble;

            if (_friendMutationInProgress || now < _nextFriendMutationAt)
            {
                RequestFailedClientRpc(OwnerClientId.ToClientRpcParams());

                return false;
            }

            _friendMutationInProgress = true;
            _nextFriendMutationAt = now + _friendMutationCooldownSeconds;

            return true;
        }

        private void EndFriendMutation(CancellationToken cancellationToken)
        {
            if (IsCurrentNetworkLifetime(cancellationToken))
            {
                _friendMutationInProgress = false;
            }
        }

        [ServerRpc]
        private void SendWhisperServerRpc(int characterId, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > _maximumWhisperLength)
            {
                WhisperRejectedClientRpc(WhisperDeliveryStatusEnum.InvalidMessage, OwnerClientId.ToClientRpcParams());

                return;
            }

            if (Time.realtimeSinceStartupAsDouble - _lastWhisperAt < _whisperCooldownSeconds)
            {
                WhisperRejectedClientRpc(WhisperDeliveryStatusEnum.RateLimited, OwnerClientId.ToClientRpcParams());

                return;
            }

            _lastWhisperAt = Time.realtimeSinceStartupAsDouble;
            SendWhisperAsync(characterId, message.Trim()).Forget();
        }

        private async UniTask SendWhisperAsync(int characterId, string message)
        {
            var cancellationToken = GetNetworkLifetimeCancellationToken();

            try
            {
                var result = await UnityWebRequestHelper.ExecuteGetAsync<AuthorizeWhisperDto>(
                    $"Friends/{characterId}/WhisperAuthorization",
                    GetPlayerSessionId(),
                    log: false,
                    cancellationToken: cancellationToken);

                if (!CanUseNetworkLifetime(cancellationToken))
                {
                    return;
                }

                if (!result.IsAllowed)
                {
                    FriendOperationClientRpc(FriendOperationTypeEnum.Invite, result.Status, result.CharacterName, OwnerClientId.ToClientRpcParams());

                    return;
                }

                if (!TryGetClientId(result.CharacterId, out var targetClientId))
                {
                    WhisperRejectedClientRpc(WhisperDeliveryStatusEnum.TargetOffline, OwnerClientId.ToClientRpcParams());

                    return;
                }

                ReceiveWhisperClientRpc(message, GetOwnerCharacterName(), targetClientId.ToClientRpcParams());
                SentWhisperClientRpc(message, result.CharacterName, OwnerClientId.ToClientRpcParams());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Whisper delivery failed: {exception.Message}");

                if (CanUseNetworkLifetime(cancellationToken))
                {
                    WhisperRejectedClientRpc(WhisperDeliveryStatusEnum.RequestFailed, OwnerClientId.ToClientRpcParams());
                }
            }
        }

        [ClientRpc]
        private void ReceiveFriendListClientRpc(string payload, ClientRpcParams rpcParams = default)
        {
            _friendList = JsonSerializer.Deserialize<FriendListDto>(payload) ?? new FriendListDto();
            FriendListUI.Instance?.Present(_friendList);
        }

        [ClientRpc]
        private void FriendOperationClientRpc(
            FriendOperationTypeEnum operation,
            FriendOperationStatusEnum status,
            string characterName,
            ClientRpcParams rpcParams = default)
        {
            FriendListUI.Instance?.ShowOperationStatus(operation, status, characterName);
        }

        [ClientRpc]
        private void FriendNotificationClientRpc(FriendNotificationTypeEnum notification, string characterName, ClientRpcParams rpcParams = default)
        {
            Local?.RequestRefresh();
            FriendListUI.Instance?.ShowNotification(notification, characterName);
        }

        [ClientRpc]
        private void RequestFailedClientRpc(ClientRpcParams rpcParams = default)
        {
            FriendListUI.Instance?.ShowRequestFailed();
        }

        [ClientRpc]
        private void ReceiveWhisperClientRpc(string message, string sender, ClientRpcParams rpcParams = default)
        {
            ChatUI.Instance?.AddWhisper(message, sender, outgoing: false);
        }

        [ClientRpc]
        private void SentWhisperClientRpc(string message, string recipient, ClientRpcParams rpcParams = default)
        {
            var chatUi = ChatUI.Instance;

            if (chatUi == null)
            {
                return;
            }

            chatUi.AddWhisper(message, recipient, outgoing: true);

            if (IsCurrentWhisper(chatUi.InputField.text, recipient, message))
            {
                chatUi.InputField.text = string.Empty;
            }

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.SendMessage);
        }

        [ClientRpc]
        private void WhisperRejectedClientRpc(WhisperDeliveryStatusEnum status, ClientRpcParams rpcParams = default)
        {
            FriendListUI.Instance?.ShowWhisperStatus(status);
        }

        private string GetPlayerSessionId()
        {
            return UserManager.Instance.GetPlayerSessionId(OwnerClientId);
        }

        private static bool IsCurrentWhisper(string command, string recipient, string message)
        {
            return TryParseWhisperCommand(command, out var currentRecipient, out var currentMessage)
                && string.Equals(currentRecipient, recipient, StringComparison.OrdinalIgnoreCase)
                && string.Equals(currentMessage, message, StringComparison.Ordinal);
        }

        private string GetOwnerCharacterName()
        {
            return UserManager.Instance.Characters.TryGetValue(OwnerClientId, out var character) ? character.Name : string.Empty;
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

            FriendListUI.Instance?.Unbind(this);
            Local = null;
        }
    }
}
