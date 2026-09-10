using System;
using System.Collections.Generic;
using Assets.Scripts.Areas.Party.Enums;
using Assets.Scripts.Areas.Party.Models;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PartyController = Assets.Scripts.Areas.Party.Mono.Party;

namespace Assets.Scripts.Areas.Party.UI
{
    public class PartyUI : MonoSingleton<PartyUI>
    {
        private const int _maxVisibleMembers = 5;
        private const int _maxVisibleInvitations = 3;

        [SerializeField] private GameObject _memberEntryPrefab;
        [SerializeField] private GameObject _invitationEntryPrefab;

        private readonly List<GameObject> _invitationRows = new List<GameObject>();
        private readonly List<GameObject> _memberRows = new List<GameObject>();

        private PartyController _controller;
        private GameObject _invitations;
        private Transform _invitationsContent;
        private TextMeshProUGUI _invitationsHeader;
        private VerticalLayoutGroup _invitationsRowsLayout;
        private LayoutElement _invitationsScrollLayout;
        private Button _leaveButton;
        private GameObject _members;
        private Transform _membersContent;
        private TextMeshProUGUI _membersHeader;
        private VerticalLayoutGroup _membersRowsLayout;
        private LayoutElement _membersScrollLayout;
        private GameObject _panel;

        protected override bool PersistBetweenScenes => false;

        protected override void Awake()
        {
            base.Awake();

            _panel = transform.Find("Panel").gameObject;
            _members = _panel.transform.Find("Members").gameObject;
            _invitations = _panel.transform.Find("Invitations").gameObject;
            _membersHeader = _members.transform.Find("Header").GetComponent<TextMeshProUGUI>();
            _invitationsHeader = _invitations.transform.Find("Header").GetComponent<TextMeshProUGUI>();
            var membersScroll = _members.transform.Find("Scroll");
            _membersContent = membersScroll.Find("Viewport/Rows");
            _membersRowsLayout = _membersContent.GetComponent<VerticalLayoutGroup>();
            _membersScrollLayout = membersScroll.GetComponent<LayoutElement>();
            var invitationsScroll = _invitations.transform.Find("Scroll");
            _invitationsContent = invitationsScroll.Find("Viewport/Rows");
            _invitationsRowsLayout = _invitationsContent.GetComponent<VerticalLayoutGroup>();
            _invitationsScrollLayout = invitationsScroll.GetComponent<LayoutElement>();
            _leaveButton = _panel.transform.Find("Header/Leave").GetComponent<Button>();
            _panel.SetActive(false);
        }

        private void Start()
        {
            _leaveButton.onClick.AddListener(Leave);
        }

        public void Bind(PartyController controller)
        {
            _controller = controller;
        }

        public void Unbind(PartyController controller)
        {
            if (_controller == controller)
            {
                _controller = null;
                _panel.SetActive(false);
            }
        }

        public void Present(PartySnapshotDto snapshot)
        {
            var members = snapshot.Members ?? Array.Empty<PartyMemberDto>();
            var invitations = snapshot.Invitations ?? Array.Empty<PartyInvitationDto>();

            ConfigureMemberRows(members);
            ConfigureInvitationRows(invitations);

            _members.SetActive(members.Length > 0);
            _invitations.SetActive(invitations.Length > 0);
            _leaveButton.gameObject.SetActive(members.Length > 0);
            _panel.SetActive(members.Length > 0 || invitations.Length > 0);

            RefreshLayout();
        }

        public void ShowOperationStatus(PartyOperationTypeEnum operation, PartyOperationStatusEnum status, string characterName)
        {
            var key = status switch
            {
                PartyOperationStatusEnum.Applied => operation switch
                {
                    PartyOperationTypeEnum.Invite => TranslateKeyEnum.PartyInviteSent,
                    PartyOperationTypeEnum.Accept => TranslateKeyEnum.PartyJoined,
                    PartyOperationTypeEnum.Decline => TranslateKeyEnum.PartyInviteDeclined,
                    PartyOperationTypeEnum.Leave => TranslateKeyEnum.PartyLeft,
                    _ => TranslateKeyEnum.PartyRequestFailed
                },
                PartyOperationStatusEnum.CharacterNotFound => TranslateKeyEnum.PartyCharacterNotFound,
                PartyOperationStatusEnum.CannotInviteSelf => TranslateKeyEnum.PartyCannotInviteSelf,
                PartyOperationStatusEnum.FriendRequired => TranslateKeyEnum.PartyFriendRequired,
                PartyOperationStatusEnum.TargetOffline => TranslateKeyEnum.PartyTargetOffline,
                PartyOperationStatusEnum.TargetAlreadyInParty => TranslateKeyEnum.PartyTargetAlreadyInParty,
                PartyOperationStatusEnum.OnlyLeaderCanInvite => TranslateKeyEnum.PartyOnlyLeaderCanInvite,
                PartyOperationStatusEnum.InvitationAlreadyPending => TranslateKeyEnum.PartyInvitationAlreadyPending,
                PartyOperationStatusEnum.InvitationNotFound => TranslateKeyEnum.PartyInvitationNotFound,
                PartyOperationStatusEnum.PartyNotFound => TranslateKeyEnum.PartyNotFound,
                _ => TranslateKeyEnum.PartyRequestFailed
            };
            var message = FormatMessage(key, characterName);
            var color = status == PartyOperationStatusEnum.Applied ? ColorUI.Success : ColorUI.Error;

            LogUI.Instance.ShowAsync(message, color: color).Forget();
        }

        public void ShowNotification(PartyNotificationTypeEnum notification, string characterName)
        {
            var key = notification switch
            {
                PartyNotificationTypeEnum.InvitationReceived => TranslateKeyEnum.PartyInviteReceived,
                PartyNotificationTypeEnum.InvitationAccepted => TranslateKeyEnum.PartyJoined,
                PartyNotificationTypeEnum.InvitationDeclined => TranslateKeyEnum.PartyInviteDeclined,
                _ => TranslateKeyEnum.PartyRequestFailed
            };

            LogUI.Instance.ShowAsync(FormatMessage(key, characterName), color: ColorUI.Information).Forget();
        }

        private void Leave()
        {
            _controller?.Leave();
        }

        private void ConfigureMemberRows(IReadOnlyList<PartyMemberDto> members)
        {
            _membersHeader.text = FormatHeader(TranslateKeyEnum.PartyMembersCount, members.Count);

            for (var index = 0; index < members.Count; index++)
            {
                var member = members[index];
                var row = GetRow(_memberRows, _memberEntryPrefab, _membersContent, index);

                row.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = member.CharacterName;
                row.transform.Find("Health").GetComponent<TextMeshProUGUI>().text = string.Format(
                    TranslateManager.Instance.GetByKey(TranslateKeyEnum.PartyHealth),
                    member.Health,
                    member.MaxHealth);
                row.transform.Find("Level").GetComponent<TextMeshProUGUI>().text = string.Format(TranslateManager.Instance.GetByKey(TranslateKeyEnum.PartyLevel), Mathf.Max(1, member.Level));
                row.transform.Find("Leader").gameObject.SetActive(member.IsLeader);
            }

            HideRows(_memberRows, members.Count);
            ResizeMembersViewport(members.Count);
        }

        private void ConfigureInvitationRows(IReadOnlyList<PartyInvitationDto> invitations)
        {
            _invitationsHeader.text = FormatHeader(TranslateKeyEnum.PartyInvitationsCount, invitations.Count);

            for (var index = 0; index < invitations.Count; index++)
            {
                var invitation = invitations[index];
                var row = GetRow(_invitationRows, _invitationEntryPrefab, _invitationsContent, index);

                row.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = FormatMessage(
                    TranslateKeyEnum.PartyInvitationFrom,
                    invitation.CharacterName);

                var accept = row.transform.Find("Accept").GetComponent<Button>();
                accept.onClick.RemoveAllListeners();
                accept.onClick.AddListener(() => _controller?.Respond(invitation.CharacterId, accept: true));

                var decline = row.transform.Find("Decline").GetComponent<Button>();
                decline.onClick.RemoveAllListeners();
                decline.onClick.AddListener(() => _controller?.Respond(invitation.CharacterId, accept: false));
            }

            HideRows(_invitationRows, invitations.Count);
            ResizeInvitationsViewport(invitations.Count);
        }

        private void RefreshLayout()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_membersContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_invitationsContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_members.transform);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_invitations.transform);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_panel.transform);
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

        private void ResizeMembersViewport(int memberCount)
        {
            ResizeViewport(
                memberCount,
                _maxVisibleMembers,
                _memberEntryPrefab,
                _membersRowsLayout,
                _membersScrollLayout);
        }

        private void ResizeInvitationsViewport(int invitationCount)
        {
            ResizeViewport(
                invitationCount,
                _maxVisibleInvitations,
                _invitationEntryPrefab,
                _invitationsRowsLayout,
                _invitationsScrollLayout);
        }

        private static void ResizeViewport(
            int itemCount,
            int maxVisibleItems,
            GameObject entryPrefab,
            VerticalLayoutGroup rowsLayout,
            LayoutElement scrollLayout)
        {
            var visibleItemCount = Mathf.Min(itemCount, maxVisibleItems);

            if (visibleItemCount == 0)
            {
                scrollLayout.preferredHeight = 0f;

                return;
            }

            var rowHeight = entryPrefab.GetComponent<LayoutElement>().preferredHeight;
            scrollLayout.preferredHeight = visibleItemCount * rowHeight + (visibleItemCount - 1) * rowsLayout.spacing;
        }

        private static string FormatHeader(TranslateKeyEnum key, int count)
        {
            return string.Format(TranslateManager.Instance.GetByKey(key), count);
        }

        private static string FormatMessage(TranslateKeyEnum key, string characterName)
        {
            var safeCharacterName = (characterName ?? string.Empty).Replace("<", "‹").Replace(">", "›");

            return string.Format(TranslateManager.Instance.GetByKey(key), safeCharacterName);
        }
    }
}
