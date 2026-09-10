using System;
using Assets.Scripts.Areas.Friends.Mono;
using Assets.Scripts.Areas.Friends.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Extensions;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Character.Mono
{
    public class Chat : NetworkBehaviour
    {
        private const int _maximumMessageLength = 200;
        private const double _messageCooldownSeconds = 0.5d;

        private double _lastMessageAt = double.NegativeInfinity;

        private void Start()
        {
            if (IsOwner)
            {
                ChatUI.Instance.InputField.onEndEdit.AddListener(Submit);
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                CheckEnter();

                CheckHide();

                CheckToggle();
            }
        }

        private void Submit(string message)
        {
            ChatUI.Instance.InputField.DeactivateInputField();

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (message.TrimStart().StartsWith("/w ", StringComparison.OrdinalIgnoreCase))
            {
                if (FriendList.Local == null || !FriendList.Local.TrySendWhisperCommand(message))
                {
                    FriendListUI.Instance?.ShowRequestFailed();
                }
            }
            else
            {
                SendServerRpc(message);
            }
        }

        private void CheckEnter()
        {
            if (!InputFocusUI.IsAnyInputFocused && ChatUI.Instance.Container.activeSelf && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                ChatUI.Instance.InputField.ActivateInputField();
            }
        }

        private void CheckHide()
        {
            if (ChatUI.Instance.InputField.isFocused && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ChatUI.Instance.InputField.text = string.Empty;
                ChatUI.Instance.InputField.DeactivateInputField();
            }
        }

        private void CheckToggle()
        {
            if (!InputFocusUI.IsAnyInputFocused && Keyboard.current.zKey.wasPressedThisFrame)
            {
                ChatUI.Instance.Toggle();
            }
        }

        [ServerRpc]
        private void SendServerRpc(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > _maximumMessageLength)
            {
                MessageRejectedClientRpc(TranslateKeyEnum.ChatMessageInvalid, OwnerClientId.ToClientRpcParams());

                return;
            }

            if (Time.realtimeSinceStartupAsDouble - _lastMessageAt < _messageCooldownSeconds)
            {
                MessageRejectedClientRpc(TranslateKeyEnum.ChatMessageRateLimited, OwnerClientId.ToClientRpcParams());

                return;
            }

            if (!UserManager.Instance.Characters.TryGetValue(OwnerClientId, out var character))
            {
                MessageRejectedClientRpc(TranslateKeyEnum.ChatMessageFailed, OwnerClientId.ToClientRpcParams());

                return;
            }

            _lastMessageAt = Time.realtimeSinceStartupAsDouble;
            var normalizedMessage = message.Trim();

            SendClientRpc(normalizedMessage, character.Name);
            MessageAcceptedClientRpc(normalizedMessage, OwnerClientId.ToClientRpcParams());
        }

        [ClientRpc]
        private void SendClientRpc(string message, string sender)
        {
            ChatUI.Instance.Add(message, sender);
        }

        [ClientRpc]
        private void MessageAcceptedClientRpc(string message, ClientRpcParams rpcParams = default)
        {
            if (string.Equals(ChatUI.Instance.InputField.text.Trim(), message, StringComparison.Ordinal))
            {
                ChatUI.Instance.InputField.text = string.Empty;
            }

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.SendMessage);
        }

        [ClientRpc]
        private void MessageRejectedClientRpc(TranslateKeyEnum key, ClientRpcParams rpcParams = default)
        {
            var message = TranslateManager.Instance.GetByKey(key);

            LogUI.Instance.ShowAsync(message, color: ColorUI.Error).Forget();
        }

        public override void OnDestroy()
        {
            if (IsOwner && ChatUI.Instance != null)
            {
                ChatUI.Instance.InputField.onEndEdit.RemoveListener(Submit);
            }

            base.OnDestroy();
        }
    }
}
