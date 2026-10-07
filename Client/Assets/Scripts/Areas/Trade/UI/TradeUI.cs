using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Areas.Character.UI;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Inventory.UI;
using Assets.Scripts.Areas.Professions.UI;
using Assets.Scripts.Areas.Quest.UI;
using Assets.Scripts.Areas.Shared.Enums;
using Assets.Scripts.Areas.Shared.Mono;
using Assets.Scripts.Areas.Shared.Subscriptions;
using Assets.Scripts.Areas.Shared.UI;
using Assets.Scripts.Areas.Trade.Enums;
using Assets.Scripts.Areas.Trade.Models;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TradeController = Assets.Scripts.Areas.Trade.Mono.Trade;

namespace Assets.Scripts.Areas.Trade.UI
{
    public class TradeUI : MonoSingleton<TradeUI>
    {
        [SerializeField] private GameObject _inventorySlotPrefab;

        private readonly List<GameObject> _mySlots = new List<GameObject>();
        private readonly List<GameObject> _partnerSlots = new List<GameObject>();
        private readonly List<string> _slotSubscriptionKeys = new List<string>();

        private TradeController _controller;
        private TradeSnapshotDto _snapshot = new TradeSnapshotDto();
        private GameObject _panel;
        private GameObject _invitation;
        private GameObject _session;
        private TextMeshProUGUI _invitationText;
        private TextMeshProUGUI _partnerName;
        private TextMeshProUGUI _myStatus;
        private TextMeshProUGUI _partnerStatus;
        private Transform _myOfferSlots;
        private Transform _partnerOfferSlots;
        private Button _acceptButton;
        private Button _declineButton;
        private Button _lockButton;
        private Button _confirmButton;
        private Button _cancelButton;
        private TextMeshProUGUI _lockLabel;
        private Button _waitingCancelButton;
        private RectTransform _myOffer;
        private Action _openAfterTrade;
        private readonly TradeInventoryProjection _inventoryProjection = new TradeInventoryProjection();
        private InventoryItemDto[] _pendingOffer;

        protected override bool PersistBetweenScenes => false;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public bool HasSession => IsOpen && _snapshot.HasSession;

        public bool CanEditOffer => HasSession && !_snapshot.MyLocked && !_snapshot.IsCommitting && _openAfterTrade == null && _controller?.IsOfferChangePending != true;

        protected override void Awake()
        {
            base.Awake();

            _panel = transform.Find("Panel").gameObject;
            _invitation = _panel.transform.Find("Invitation").gameObject;
            _session = _panel.transform.Find("Session").gameObject;
            _invitationText = _invitation.transform.Find("Message").GetComponent<TextMeshProUGUI>();
            _acceptButton = _invitation.transform.Find("Actions/Accept").GetComponent<Button>();
            _declineButton = _invitation.transform.Find("Actions/Decline").GetComponent<Button>();
            _partnerName = _session.transform.Find("Partner").GetComponent<TextMeshProUGUI>();
            _myStatus = _session.transform.Find("Offers/MyOffer/Status").GetComponent<TextMeshProUGUI>();
            _partnerStatus = _session.transform.Find("Offers/PartnerOffer/Status").GetComponent<TextMeshProUGUI>();
            _myOffer = (RectTransform)_session.transform.Find("Offers/MyOffer");
            _myOfferSlots = _myOffer.Find("Scroll/Viewport/Content");
            _partnerOfferSlots = _session.transform.Find("Offers/PartnerOffer/Scroll/Viewport/Content");
            _lockButton = _session.transform.Find("Actions/Lock").GetComponent<Button>();
            _confirmButton = _session.transform.Find("Actions/Confirm").GetComponent<Button>();
            _cancelButton = _session.transform.Find("Actions/Cancel").GetComponent<Button>();
            _waitingCancelButton = _invitation.transform.Find("Actions/Cancel").GetComponent<Button>();
            _lockLabel = _lockButton.GetComponentInChildren<TextMeshProUGUI>();

            _panel.SetActive(false);
        }

        private void Start()
        {
            _acceptButton.onClick.AddListener(AcceptInvitation);
            _declineButton.onClick.AddListener(DeclineInvitation);
            _lockButton.onClick.AddListener(() => _controller?.ToggleLock());
            _confirmButton.onClick.AddListener(() => _controller?.Confirm());
            _cancelButton.onClick.AddListener(() => _controller?.Cancel());
            _waitingCancelButton.onClick.AddListener(() => _controller?.Cancel());
        }

        protected override void OnDestroy()
        {
            foreach (var key in _slotSubscriptionKeys)
            {
                OnPointerEnterSubscription.Instance.Unsubscribe(key);
                OnPointerExitSubscription.Instance.Unsubscribe(key);
            }

            base.OnDestroy();
        }

        private void Update()
        {
            if (_panel.activeSelf && Keyboard.current.escapeKey.wasPressedThisFrame && !InputFocusUI.IsAnyInputFocused)
            {
                _controller?.Cancel();
            }
        }

        private void LateUpdate()
        {
            if (IsOpen)
            {
                if (_openAfterTrade != null && !_snapshot.IsCommitting)
                {
                    _controller?.Cancel();
                }
            }
        }

        public bool OpenAfterTrade(Action openPanel)
        {
            if (!IsOpen)
            {
                return false;
            }

            _openAfterTrade = openPanel;
            _controller?.Cancel();

            return true;
        }

        public void Bind(TradeController controller)
        {
            _controller = controller;
        }

        public void Unbind(TradeController controller)
        {
            if (_controller == controller)
            {
                _controller = null;
                _openAfterTrade = null;
                _pendingOffer = null;
                _snapshot = new TradeSnapshotDto();
                _panel.SetActive(false);
                InventoryUI.Instance?.RefreshTradeInventory();
            }
        }

        public void Present(TradeSnapshotDto snapshot)
        {
            var wasOpen = IsOpen;
            var hadSession = _snapshot.HasSession;
            _snapshot = snapshot ?? new TradeSnapshotDto();

            var hasIncoming = _snapshot.Invitation != null;
            var hasOutgoing = _snapshot.OutgoingInvitation != null;
            var showInvitation = !_snapshot.HasSession && (hasIncoming || hasOutgoing);

            _invitation.SetActive(showInvitation);
            _session.SetActive(_snapshot.HasSession);
            _panel.SetActive(showInvitation || _snapshot.HasSession);

            if (!_snapshot.HasSession)
            {
                _pendingOffer = null;
            }

            InventoryUI.Instance?.RefreshTradeInventory();

            if (IsOpen && !wasOpen)
            {
                CraftingUI.Instance?.Hide();
                QuestUI.Instance?.Hide();
                MerchantUI.Instance?.Hide();
                GearUI.Instance?.Hide();
                CharacterUI.Instance?.Hide();
            }

            if (_snapshot.HasSession && !hadSession && InventoryUI.Instance?.Inventory.activeSelf == false)
            {
                InventoryUI.Instance.Toggle();
            }

            if (showInvitation)
            {
                ConfigureInvitation(hasIncoming);
            }

            if (_snapshot.HasSession)
            {
                ConfigureSession();
            }

            if (!IsOpen)
            {
                var openPanel = _openAfterTrade;
                _openAfterTrade = null;
                openPanel?.Invoke();
            }
        }

        public bool IsMyOfferDropTarget(GameObject target)
        {
            return target != null
                && CanEditOffer
                && target.transform.IsChildOf(_myOffer);
        }

        public void AddToOffer(InventoryItemDto item, int sourceSlotIndex)
        {
            if (!HasSession || _snapshot.IsCommitting || _openAfterTrade != null)
            {
                return;
            }

            if (_snapshot.MyLocked)
            {
                ShowOperationStatus(TradeOperationTypeEnum.Offer, TradeOperationStatusEnum.OfferLocked, string.Empty);

                return;
            }

            _controller?.AddItemToOffer(item, sourceSlotIndex);
        }

        public void RemoveFromOffer(InventoryItemEnum type)
        {
            if (CanEditOffer)
            {
                _controller?.RemoveItemFromOffer(type);
            }
        }

        public void PreviewOfferAddition(InventoryItemDto[] offer, int sourceSlotIndex, InventoryItemDto item)
        {
            _pendingOffer = offer;
            _inventoryProjection.PreferSource(sourceSlotIndex, item);
            InventoryUI.Instance?.RefreshTradeInventory();
        }

        public void ClearPendingOffer()
        {
            _pendingOffer = null;
        }

        public InventoryItemDto[] GetAvailableInventoryItems(IList<InventoryItemDto> inventory)
        {
            var offer = HasSession ? _pendingOffer ?? _snapshot.MyOffer : Array.Empty<InventoryItemDto>();

            return _inventoryProjection.Project(inventory, offer ?? Array.Empty<InventoryItemDto>());
        }

        public bool IsInventorySlotReserved(int slotIndex) => HasSession && _inventoryProjection.IsReserved(slotIndex);

        public void HidePreviews()
        {
            foreach (var slot in _mySlots.Concat(_partnerSlots))
            {
                slot.transform.Find("Preview").gameObject.SetActive(false);
            }
        }

        public void ShowOperationStatus(TradeOperationTypeEnum operation, TradeOperationStatusEnum status, string characterName)
        {
            if (status == TradeOperationStatusEnum.Applied
                && operation is TradeOperationTypeEnum.Offer or TradeOperationTypeEnum.Lock or TradeOperationTypeEnum.Unlock or TradeOperationTypeEnum.Confirm)
            {
                return;
            }

            var key = status switch
            {
                TradeOperationStatusEnum.Applied => operation switch
                {
                    TradeOperationTypeEnum.Invite => TranslateKeyEnum.TradeInviteSent,
                    TradeOperationTypeEnum.Accept => TranslateKeyEnum.TradeStarted,
                    TradeOperationTypeEnum.Decline => TranslateKeyEnum.TradeInviteDeclined,
                    TradeOperationTypeEnum.Cancel => TranslateKeyEnum.TradeCancelled,
                    _ => TranslateKeyEnum.TradeRequestFailed
                },
                TradeOperationStatusEnum.CannotTradeSelf => TranslateKeyEnum.TradeCannotTradeSelf,
                TradeOperationStatusEnum.FriendRequired => TranslateKeyEnum.TradeFriendRequired,
                TradeOperationStatusEnum.TargetOffline => TranslateKeyEnum.TradeTargetOffline,
                TradeOperationStatusEnum.TargetBusy => TranslateKeyEnum.TradeTargetBusy,
                TradeOperationStatusEnum.InvitationAlreadyPending => TranslateKeyEnum.TradeInvitationAlreadyPending,
                TradeOperationStatusEnum.InvitationNotFound => TranslateKeyEnum.TradeInvitationNotFound,
                TradeOperationStatusEnum.TradeNotFound => TranslateKeyEnum.TradeNotFound,
                TradeOperationStatusEnum.OfferLocked => TranslateKeyEnum.TradeOfferLocked,
                TradeOperationStatusEnum.InvalidOffer => TranslateKeyEnum.TradeInvalidOffer,
                TradeOperationStatusEnum.OfferLimitReached => TranslateKeyEnum.TradeOfferLimitReached,
                TradeOperationStatusEnum.WaitingForLocks => TranslateKeyEnum.TradeWaitingForLocks,
                TradeOperationStatusEnum.InventoryFull => TranslateKeyEnum.TradeInventoryFull,
                TradeOperationStatusEnum.ItemsUnavailable => TranslateKeyEnum.TradeItemsUnavailable,
                TradeOperationStatusEnum.InventoryChanged => TranslateKeyEnum.TradeInventoryChanged,
                TradeOperationStatusEnum.CommitRetrying => TranslateKeyEnum.TradeCommitRetrying,
                _ => TranslateKeyEnum.TradeRequestFailed
            };
            var message = FormatMessage(key, characterName);
            var color = status == TradeOperationStatusEnum.Applied ? ColorUI.Success : ColorUI.Error;

            LogUI.Instance.ShowAsync(message, color: color).Forget();
        }

        public void ShowNotification(TradeNotificationTypeEnum notification, string characterName)
        {
            var key = notification switch
            {
                TradeNotificationTypeEnum.InvitationReceived => TranslateKeyEnum.TradeInviteReceived,
                TradeNotificationTypeEnum.InvitationAccepted => TranslateKeyEnum.TradeStarted,
                TradeNotificationTypeEnum.InvitationDeclined => TranslateKeyEnum.TradeInviteDeclined,
                TradeNotificationTypeEnum.Cancelled => TranslateKeyEnum.TradeCancelledByPartner,
                TradeNotificationTypeEnum.Completed => TranslateKeyEnum.TradeCompleted,
                _ => TranslateKeyEnum.TradeRequestFailed
            };

            LogUI.Instance.ShowAsync(FormatMessage(key, characterName), color: ColorUI.Information).Forget();
        }

        private void ConfigureInvitation(bool incoming)
        {
            var invitation = incoming ? _snapshot.Invitation : _snapshot.OutgoingInvitation;
            var key = incoming ? TranslateKeyEnum.TradeInvitationFrom : TranslateKeyEnum.TradeWaitingForPlayer;

            _invitationText.text = FormatMessage(key, invitation.CharacterName);
            _acceptButton.gameObject.SetActive(incoming);
            _declineButton.gameObject.SetActive(incoming);
            _waitingCancelButton.gameObject.SetActive(!incoming);
        }

        private void ConfigureSession()
        {
            _partnerName.text = FormatMessage(TranslateKeyEnum.TradeWithPlayer, _snapshot.PartnerName);
            ConfigureOffer(_mySlots, _myOfferSlots, _snapshot.MyOffer, mine: true);
            ConfigureOffer(_partnerSlots, _partnerOfferSlots, _snapshot.PartnerOffer, mine: false);

            _myStatus.text = GetStatus(_snapshot.MyLocked, _snapshot.MyConfirmed);
            _partnerStatus.text = GetStatus(_snapshot.PartnerLocked, _snapshot.PartnerConfirmed);
            _myStatus.color = GetStatusColor(_snapshot.MyLocked, _snapshot.MyConfirmed);
            _partnerStatus.color = GetStatusColor(_snapshot.PartnerLocked, _snapshot.PartnerConfirmed);
            _lockLabel.text = TranslateManager.Instance.GetByKey(_snapshot.MyLocked ? TranslateKeyEnum.TradeUnlock : TranslateKeyEnum.TradeLock);
            _lockButton.interactable = !_snapshot.IsCommitting && !_snapshot.MyConfirmed;
            _confirmButton.interactable = !_snapshot.IsCommitting
                && _snapshot.MyLocked
                && _snapshot.PartnerLocked
                && !_snapshot.MyConfirmed;
            _cancelButton.interactable = !_snapshot.IsCommitting;
        }

        private void ConfigureOffer(List<GameObject> slots, Transform parent, IReadOnlyList<InventoryItemDto> items, bool mine)
        {
            items ??= Array.Empty<InventoryItemDto>();

            while (slots.Count < items.Count)
            {
                slots.Add(CreateOfferSlot(parent, mine, slots.Count));
            }

            for (var index = 0; index < slots.Count; index++)
            {
                var slot = slots[index];
                slot.SetActive(index < items.Count);

                if (index >= items.Count)
                {
                    continue;
                }

                var image = slot.transform.Find("Background").GetComponent<RawImage>();
                var count = slot.transform.Find("Text").GetComponent<TextMeshProUGUI>();
                var preview = slot.transform.Find("Preview").gameObject;
                var button = slot.GetComponent<ButtonUI>();

                preview.SetActive(false);
                image.color = ColorUI.White;
                image.texture = InventoryUI.Instance.Textures[items[index].Type];
                count.gameObject.SetActive(true);
                count.text = items[index].Count.ToString();
                button.onClick.RemoveAllListeners();
                button.OnRightClick.RemoveAllListeners();

                var item = items[index];
                preview.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = TranslateManager.Instance.GetByKey($"{item.Type}Title");
                preview.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = InventoryUI.Instance.PrepareDescription(item);

                if (mine)
                {
                    button.onClick.AddListener(() => RemoveFromOffer(item.Type));
                    button.OnRightClick.AddListener(() => RemoveFromOffer(item.Type));
                }
            }

            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)parent);
        }

        private GameObject CreateOfferSlot(Transform parent, bool mine, int index)
        {
            var slot = Instantiate(_inventorySlotPrefab, parent, worldPositionStays: false);
            var preview = slot.transform.Find("Preview").gameObject;
            var count = slot.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            var key = slot.GetInstanceID().ToString();
            _slotSubscriptionKeys.Add(key);

            count.enableAutoSizing = true;
            count.fontSizeMin = 8;
            count.fontSizeMax = 22;
            count.textWrappingMode = TextWrappingModes.NoWrap;

            foreach (var graphic in preview.GetComponentsInChildren<MaskableGraphic>(includeInactive: true))
            {
                graphic.maskable = false;
                graphic.raycastTarget = false;
            }

            OnPointerEnterSubscription.Instance.Subscribe(key, _ =>
            {
                if (!InventoryUI.Instance.IsDragging && slot.activeInHierarchy)
                {
                    preview.SetActive(true);
                }
            });
            OnPointerExitSubscription.Instance.Subscribe(key, _ => preview.SetActive(false));

            if (mine)
            {
                InventoryUI.Instance.ConfigureTradeOfferDrag(
                    slot,
                    slot.transform.Find("Background").GetComponent<RawImage>(),
                    count,
                    () => CanEditOffer ? _snapshot.MyOffer.ElementAtOrDefault(index) : null);
            }

            slot.name = mine ? $"MyOffer{index + 1}" : $"PartnerOffer{index + 1}";

            return slot;
        }

        private void AcceptInvitation()
        {
            if (_snapshot.Invitation != null)
            {
                _controller?.Respond(_snapshot.Invitation.CharacterId, accept: true);
            }
        }

        private void DeclineInvitation()
        {
            if (_snapshot.Invitation != null)
            {
                _controller?.Respond(_snapshot.Invitation.CharacterId, accept: false);
            }
        }

        private static string GetStatus(bool locked, bool confirmed)
        {
            var key = confirmed
                ? TranslateKeyEnum.TradeConfirmed
                : locked
                    ? TranslateKeyEnum.TradeLocked
                    : TranslateKeyEnum.TradeEditing;

            return TranslateManager.Instance.GetByKey(key);
        }

        private static Color GetStatusColor(bool locked, bool confirmed)
        {
            return confirmed ? ColorUI.Success : locked ? ColorUI.Warning : ColorUI.TextSecondary;
        }

        private static string FormatMessage(TranslateKeyEnum key, string characterName)
        {
            var safeCharacterName = (characterName ?? string.Empty).Replace("<", "‹").Replace(">", "›");

            return string.Format(TranslateManager.Instance.GetByKey(key), safeCharacterName);
        }

    }
}
