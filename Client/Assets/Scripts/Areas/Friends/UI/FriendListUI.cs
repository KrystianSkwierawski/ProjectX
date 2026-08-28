using System;
using System.Collections.Generic;
using Assets.Scripts.Areas.Friends.Enums;
using Assets.Scripts.Areas.Friends.Models;
using Assets.Scripts.Areas.Friends.Mono;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Areas.Friends.UI
{
    public class FriendListUI : MonoSingleton<FriendListUI>
    {
        [SerializeField] private GameObject _friendEntryPrefab;
        [SerializeField] private GameObject _invitationEntryPrefab;
        [SerializeField] private GameObject _outgoingInvitationEntryPrefab;

        private readonly List<GameObject> _friendRows = new List<GameObject>();
        private readonly List<GameObject> _incomingRows = new List<GameObject>();
        private readonly List<GameObject> _outgoingRows = new List<GameObject>();

        private FriendList _controller;
        private GameObject _panel;
        private RectTransform _contentRect;
        private ScrollRect _contentScroll;
        private TMP_InputField _inviteInput;
        private Transform _friendsContent;
        private Transform _incomingContent;
        private Transform _outgoingContent;
        private TextMeshProUGUI _friendsHeader;
        private TextMeshProUGUI _incomingHeader;
        private TextMeshProUGUI _outgoingHeader;

        protected override bool PersistBetweenScenes => false;

        public bool IsOpen => _panel.activeSelf;

        protected override void Awake()
        {
            base.Awake();

            _panel = transform.Find("Panel").gameObject;
            _inviteInput = _panel.transform.Find("Invite/Input").GetComponent<TMP_InputField>();
            _contentScroll = _panel.transform.Find("ContentScroll").GetComponent<ScrollRect>();
            _contentRect = _contentScroll.content;
            var content = _contentRect.transform;

            _friendsHeader = content.Find("Friends/Header").GetComponent<TextMeshProUGUI>();
            _incomingHeader = content.Find("Incoming/Header").GetComponent<TextMeshProUGUI>();
            _outgoingHeader = content.Find("Outgoing/Header").GetComponent<TextMeshProUGUI>();
            _friendsContent = content.Find("Friends/Rows");
            _incomingContent = content.Find("Incoming/Rows");
            _outgoingContent = content.Find("Outgoing/Rows");
        }

        private void Start()
        {
            _panel.transform.Find("Header/Close").GetComponent<Button>().onClick.AddListener(Hide);
            _panel.transform.Find("Header/Refresh").GetComponent<Button>().onClick.AddListener(RequestRefresh);
            _panel.transform.Find("Invite/Button").GetComponent<Button>().onClick.AddListener(Invite);
            _inviteInput.onSubmit.AddListener(_ => Invite());

            _panel.SetActive(false);
        }

        public void Bind(FriendList controller)
        {
            _controller = controller;
        }

        public void Unbind(FriendList controller)
        {
            if (_controller == controller)
            {
                _controller = null;
                Hide();
            }
        }

        public void Toggle()
        {
            _panel.SetActive(!_panel.activeSelf);

            if (_panel.activeSelf)
            {
                RefreshLayout();
                RequestRefresh();
            }
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        public void Present(FriendListDto data)
        {
            ConfigureFriendRows(data.Friends ?? Array.Empty<FriendDto>());
            ConfigureIncomingRows(data.IncomingInvitations ?? Array.Empty<FriendInvitationDto>());
            ConfigureOutgoingRows(data.OutgoingInvitations ?? Array.Empty<FriendInvitationDto>());
            RefreshLayout();
        }

        public void BeginWhisper(string characterName)
        {
            ChatUI.Instance.BeginWhisper(characterName);
        }

        public void ShowOperationStatus(FriendOperationTypeEnum operation, FriendOperationStatusEnum status, string characterName)
        {
            var key = status switch
            {
                FriendOperationStatusEnum.Applied => operation switch
                {
                    FriendOperationTypeEnum.Invite => TranslateKeyEnum.FriendInviteSent,
                    FriendOperationTypeEnum.Accept => TranslateKeyEnum.FriendInviteAccepted,
                    FriendOperationTypeEnum.Decline => TranslateKeyEnum.FriendInviteDeclined,
                    FriendOperationTypeEnum.Remove => TranslateKeyEnum.FriendRemoved,
                    _ => TranslateKeyEnum.FriendRequestFailed
                },
                FriendOperationStatusEnum.CharacterNotFound => TranslateKeyEnum.FriendCharacterNotFound,
                FriendOperationStatusEnum.CannotInviteSelf => TranslateKeyEnum.FriendCannotInviteSelf,
                FriendOperationStatusEnum.AlreadyFriends => TranslateKeyEnum.FriendAlreadyFriends,
                FriendOperationStatusEnum.InvitationAlreadyPending => TranslateKeyEnum.FriendInvitationAlreadyPending,
                FriendOperationStatusEnum.InvitationNotFound => TranslateKeyEnum.FriendInvitationNotFound,
                FriendOperationStatusEnum.FriendshipNotFound => TranslateKeyEnum.FriendshipNotFound,
                FriendOperationStatusEnum.WhisperNotAllowed => TranslateKeyEnum.FriendWhisperNotAllowed,
                _ => TranslateKeyEnum.FriendRequestFailed
            };
            var message = FormatMessage(key, characterName);
            var color = status == FriendOperationStatusEnum.Applied ? ColorUI.Success : ColorUI.Error;

            LogUI.Instance.ShowAsync(message, color: color).Forget();
        }

        public void ShowNotification(FriendNotificationTypeEnum notification, string characterName)
        {
            var key = notification switch
            {
                FriendNotificationTypeEnum.InvitationReceived => TranslateKeyEnum.FriendInviteReceived,
                FriendNotificationTypeEnum.InvitationAccepted => TranslateKeyEnum.FriendInviteAccepted,
                FriendNotificationTypeEnum.InvitationDeclined => TranslateKeyEnum.FriendInviteDeclined,
                FriendNotificationTypeEnum.FriendRemoved => TranslateKeyEnum.FriendRemoved,
                _ => TranslateKeyEnum.FriendRequestFailed
            };

            LogUI.Instance.ShowAsync(FormatMessage(key, characterName), color: ColorUI.Information).Forget();
        }

        public void ShowWhisperStatus(WhisperDeliveryStatusEnum status)
        {
            var key = status switch
            {
                WhisperDeliveryStatusEnum.InvalidMessage => TranslateKeyEnum.FriendWhisperInvalid,
                WhisperDeliveryStatusEnum.TargetOffline => TranslateKeyEnum.FriendWhisperOffline,
                WhisperDeliveryStatusEnum.RateLimited => TranslateKeyEnum.FriendWhisperRateLimited,
                _ => TranslateKeyEnum.FriendRequestFailed
            };

            LogUI.Instance.ShowAsync(TranslateManager.Instance.GetByKey(key), color: ColorUI.Error).Forget();
        }

        public void ShowRequestFailed()
        {
            LogUI.Instance.ShowAsync(TranslateManager.Instance.GetByKey(TranslateKeyEnum.FriendRequestFailed), color: ColorUI.Error).Forget();
        }

        private void Invite()
        {
            var characterName = _inviteInput.text.Trim();

            if (string.IsNullOrWhiteSpace(characterName))
            {
                return;
            }

            _controller?.Invite(characterName);
            _inviteInput.text = string.Empty;
        }

        private void RequestRefresh()
        {
            _controller?.RequestRefresh();
        }

        private void RefreshLayout()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_incomingContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_friendsContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_outgoingContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_incomingContent.parent);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_friendsContent.parent);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_outgoingContent.parent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
            Canvas.ForceUpdateCanvases();
            _contentScroll.verticalNormalizedPosition = 1f;
        }

        private void ConfigureFriendRows(IReadOnlyList<FriendDto> friends)
        {
            _friendsHeader.text = FormatHeader(TranslateKeyEnum.Friends, friends.Count);

            for (var index = 0; index < friends.Count; index++)
            {
                var friend = friends[index];
                var row = GetRow(_friendRows, _friendEntryPrefab, _friendsContent, index);

                row.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = friend.CharacterName;

                var status = row.transform.Find("Status").GetComponent<TextMeshProUGUI>();
                status.text = TranslateManager.Instance.GetByKey(friend.IsOnline ? TranslateKeyEnum.FriendOnline : TranslateKeyEnum.FriendOffline);
                status.color = friend.IsOnline ? ColorUI.TextPrimary : ColorUI.TextMuted;

                var whisper = row.transform.Find("Whisper").GetComponent<Button>();
                whisper.interactable = friend.IsOnline;
                whisper.onClick.RemoveAllListeners();
                whisper.onClick.AddListener(() => BeginWhisper(friend.CharacterName));

                var remove = row.transform.Find("Remove").GetComponent<Button>();
                remove.onClick.RemoveAllListeners();
                remove.onClick.AddListener(() => _controller?.Remove(friend.CharacterId));
            }

            HideRows(_friendRows, friends.Count);
        }

        private void ConfigureIncomingRows(IReadOnlyList<FriendInvitationDto> invitations)
        {
            _incomingHeader.text = FormatHeader(TranslateKeyEnum.FriendIncoming, invitations.Count);

            for (var index = 0; index < invitations.Count; index++)
            {
                var invitation = invitations[index];
                var row = GetRow(_incomingRows, _invitationEntryPrefab, _incomingContent, index);

                row.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = invitation.CharacterName;

                var accept = row.transform.Find("Accept").GetComponent<Button>();
                accept.onClick.RemoveAllListeners();
                accept.onClick.AddListener(() => _controller?.Respond(invitation.CharacterId, accept: true));

                var decline = row.transform.Find("Decline").GetComponent<Button>();
                decline.onClick.RemoveAllListeners();
                decline.onClick.AddListener(() => _controller?.Respond(invitation.CharacterId, accept: false));
            }

            HideRows(_incomingRows, invitations.Count);
        }

        private void ConfigureOutgoingRows(IReadOnlyList<FriendInvitationDto> invitations)
        {
            _outgoingHeader.text = FormatHeader(TranslateKeyEnum.FriendOutgoing, invitations.Count);

            for (var index = 0; index < invitations.Count; index++)
            {
                var invitation = invitations[index];
                var row = GetRow(_outgoingRows, _outgoingInvitationEntryPrefab, _outgoingContent, index);

                row.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = invitation.CharacterName;
            }

            HideRows(_outgoingRows, invitations.Count);
        }

        private static GameObject GetRow(IList<GameObject> rows, GameObject prefab, Transform parent, int index)
        {
            if (rows.Count <= index)
            {
                rows.Add(Instantiate(prefab, parent, worldPositionStays: false));
            }

            var row = rows[index];
            row.SetActive(true);

            return row;
        }

        private static void HideRows(IReadOnlyList<GameObject> rows, int visibleCount)
        {
            for (var index = visibleCount; index < rows.Count; index++)
            {
                rows[index].SetActive(false);
            }
        }

        private static string FormatHeader(TranslateKeyEnum key, int count)
        {
            return $"{TranslateManager.Instance.GetByKey(key)} ({count})";
        }

        private static string FormatMessage(TranslateKeyEnum key, string characterName)
        {
            var safeCharacterName = (characterName ?? string.Empty).Replace("<", "‹").Replace(">", "›");

            return string.Format(TranslateManager.Instance.GetByKey(key), safeCharacterName);
        }
    }
}
