using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Areas.Character.Mono
{
    public class Chat : NetworkBehaviour
    {
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
                CheckEsc();
            }
        }

        private void Submit(string message)
        {
            ChatUI.Instance.InputField.DeactivateInputField();

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            SendServerRpc(message, PlayerUI.Instance.PlayerNameText.text);

            ChatUI.Instance.InputField.text = string.Empty;

            AudioManager.Instance.TryPlayOneShot(AudioTypeEnum.SendMessage);
        }

        private void CheckEnter()
        {
            if (!ChatUI.Instance.InputField.isFocused && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                ChatUI.Instance.InputField.ActivateInputField();
            }
        }

        private void CheckEsc()
        {
            if (ChatUI.Instance.InputField.isFocused && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ChatUI.Instance.InputField.text = string.Empty;
                ChatUI.Instance.InputField.DeactivateInputField();
            }
        }

        [ServerRpc]
        private void SendServerRpc(string message, string sender)
        {
            // TODO: validate?
            // TODO: log?
            SendClientRpc(message, sender);
        }

        [ClientRpc]
        private void SendClientRpc(string message, string sender)
        {
            ChatUI.Instance.Add(message, sender);
        }
    }
}
