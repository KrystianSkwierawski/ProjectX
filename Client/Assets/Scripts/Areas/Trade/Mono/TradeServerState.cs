using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Areas.Inventory.Enums;
using Assets.Scripts.Areas.Inventory.Models;
using Assets.Scripts.Areas.Trade.Enums;
using UnityEngine;

namespace Assets.Scripts.Areas.Trade.Mono
{
    public static class TradeServerState
    {
        // Product/review bound rather than an inventory constraint: it keeps the offer concise
        // and bounds accepted server-side trade state before the atomic inventory transaction.
        public const int MaximumOfferItemTypes = 6;

        private static readonly Dictionary<ulong, TradeSession> _sessionsByPlayer = new Dictionary<ulong, TradeSession>();
        private static readonly Dictionary<ulong, ulong> _invitersByTarget = new Dictionary<ulong, ulong>();
        private static readonly Dictionary<ulong, int> _inventoryMutationsByPlayer = new Dictionary<ulong, int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            ResetForServerShutdown();
        }

        internal static void ResetForServerShutdown()
        {
            _sessionsByPlayer.Clear();
            _invitersByTarget.Clear();
            _inventoryMutationsByPlayer.Clear();
        }

        public static TradeOperationStatusEnum Invite(ulong inviterClientId, ulong targetClientId)
        {
            if (inviterClientId == targetClientId)
            {
                return TradeOperationStatusEnum.CannotTradeSelf;
            }

            if (IsBusy(inviterClientId) || IsBusy(targetClientId))
            {
                return _invitersByTarget.TryGetValue(targetClientId, out var inviter)
                    && inviter == inviterClientId
                    ? TradeOperationStatusEnum.InvitationAlreadyPending
                    : TradeOperationStatusEnum.TargetBusy;
            }

            _invitersByTarget[targetClientId] = inviterClientId;

            return TradeOperationStatusEnum.Applied;
        }

        public static TradeOperationStatusEnum Respond(ulong targetClientId, ulong inviterClientId, bool accept)
        {
            if (!_invitersByTarget.TryGetValue(targetClientId, out var storedInviterClientId)
                || storedInviterClientId != inviterClientId)
            {
                return TradeOperationStatusEnum.InvitationNotFound;
            }

            _invitersByTarget.Remove(targetClientId);

            if (!accept)
            {
                return TradeOperationStatusEnum.Applied;
            }

            if (_sessionsByPlayer.ContainsKey(inviterClientId) || _sessionsByPlayer.ContainsKey(targetClientId))
            {
                return TradeOperationStatusEnum.TargetBusy;
            }

            var session = new TradeSession(Guid.NewGuid(), inviterClientId, targetClientId);
            _sessionsByPlayer[inviterClientId] = session;
            _sessionsByPlayer[targetClientId] = session;

            RemoveInvitationsFor(inviterClientId);
            RemoveInvitationsFor(targetClientId);

            return TradeOperationStatusEnum.Applied;
        }

        public static TradeOperationStatusEnum SetOffer(ulong clientId, IEnumerable<InventoryItemDto> items)
        {
            if (!TryGetSession(clientId, out var session))
            {
                return TradeOperationStatusEnum.TradeNotFound;
            }

            if (session.IsCommitting || session.IsLocked(clientId))
            {
                return TradeOperationStatusEnum.OfferLocked;
            }

            if (!TryNormalizeOffer(items, out var offer, out var status))
            {
                return status;
            }

            session.SetOffer(clientId, offer);
            session.FirstConfirmed = false;
            session.SecondConfirmed = false;

            return TradeOperationStatusEnum.Applied;
        }

        public static TradeOperationStatusEnum SetLocked(ulong clientId, bool locked)
        {
            if (!TryGetSession(clientId, out var session))
            {
                return TradeOperationStatusEnum.TradeNotFound;
            }

            if (session.IsCommitting)
            {
                return TradeOperationStatusEnum.OfferLocked;
            }

            session.SetLocked(clientId, locked);
            session.FirstConfirmed = false;
            session.SecondConfirmed = false;

            return TradeOperationStatusEnum.Applied;
        }

        public static TradeOperationStatusEnum Confirm(ulong clientId)
        {
            if (!TryGetSession(clientId, out var session))
            {
                return TradeOperationStatusEnum.TradeNotFound;
            }

            if (session.IsCommitting)
            {
                return TradeOperationStatusEnum.OfferLocked;
            }

            if (!session.FirstLocked || !session.SecondLocked)
            {
                return TradeOperationStatusEnum.WaitingForLocks;
            }

            if (session.FirstOffer.Count == 0 && session.SecondOffer.Count == 0)
            {
                return TradeOperationStatusEnum.InvalidOffer;
            }

            session.SetConfirmed(clientId, true);

            if (session.FirstConfirmed && session.SecondConfirmed)
            {
                session.IsCommitting = true;
            }

            return TradeOperationStatusEnum.Applied;
        }

        public static TradeOperationStatusEnum Cancel(ulong clientId, out ulong partnerClientId)
        {
            partnerClientId = default;

            if (!TryGetSession(clientId, out var session))
            {
                return RemoveInvitationsFor(clientId)
                    ? TradeOperationStatusEnum.Applied
                    : TradeOperationStatusEnum.TradeNotFound;
            }

            if (session.IsCommitting)
            {
                return TradeOperationStatusEnum.OfferLocked;
            }

            partnerClientId = session.GetPartner(clientId);
            RemoveSession(session);

            return TradeOperationStatusEnum.Applied;
        }

        public static bool TryGetSession(ulong clientId, out TradeSession session)
        {
            return _sessionsByPlayer.TryGetValue(clientId, out session);
        }

        public static bool TryGetInvitation(ulong targetClientId, out ulong inviterClientId)
        {
            return _invitersByTarget.TryGetValue(targetClientId, out inviterClientId);
        }

        public static bool TryGetOutgoingInvitation(ulong inviterClientId, out ulong targetClientId)
        {
            foreach (var invitation in _invitersByTarget)
            {
                if (invitation.Value == inviterClientId)
                {
                    targetClientId = invitation.Key;

                    return true;
                }
            }

            targetClientId = default;

            return false;
        }

        public static bool IsInventoryReserved(ulong clientId)
        {
            return TryGetSession(clientId, out var session)
                && (session.IsLocked(clientId) || session.IsCommitting);
        }

        public static IDisposable TrackInventoryMutation(ulong clientId)
        {
            _inventoryMutationsByPlayer.TryGetValue(clientId, out var count);
            _inventoryMutationsByPlayer[clientId] = checked(count + 1);

            return new InventoryMutationScope(clientId);
        }

        public static bool HasPendingInventoryMutation(ulong clientId)
        {
            return _inventoryMutationsByPlayer.ContainsKey(clientId);
        }

        public static bool TryGetCommit(ulong clientId, out TradeCommit commit)
        {
            commit = null;

            if (!TryGetSession(clientId, out var session) || !session.IsCommitting)
            {
                return false;
            }

            commit = new TradeCommit(
                session.Id,
                session.FirstClientId,
                session.SecondClientId,
                CloneItems(session.FirstOffer),
                CloneItems(session.SecondOffer));

            return true;
        }

        public static bool IsCommitPending(Guid sessionId)
        {
            return _sessionsByPlayer.Values.Any(x => x.Id == sessionId && x.IsCommitting);
        }

        public static bool IsCommitPendingForPlayer(ulong clientId)
        {
            return TryGetSession(clientId, out var session) && session.IsCommitting;
        }

        public static bool Complete(Guid sessionId)
        {
            var session = _sessionsByPlayer.Values.FirstOrDefault(x => x.Id == sessionId);

            if (session == null)
            {
                return false;
            }

            RemoveSession(session);

            return true;
        }

        public static bool FailCommit(Guid sessionId)
        {
            var session = _sessionsByPlayer.Values.FirstOrDefault(x => x.Id == sessionId);

            if (session == null)
            {
                return false;
            }

            session.IsCommitting = false;
            session.FirstLocked = false;
            session.SecondLocked = false;
            session.FirstConfirmed = false;
            session.SecondConfirmed = false;

            return true;
        }

        public static ulong? RemovePlayer(ulong clientId)
        {
            RemoveInvitationsFor(clientId);

            if (!TryGetSession(clientId, out var session))
            {
                return null;
            }

            var partnerClientId = session.GetPartner(clientId);

            if (!session.IsCommitting)
            {
                RemoveSession(session);
            }

            return partnerClientId;
        }

        private static bool IsBusy(ulong clientId)
        {
            return _sessionsByPlayer.ContainsKey(clientId)
                || _invitersByTarget.ContainsKey(clientId)
                || _invitersByTarget.Values.Contains(clientId);
        }

        private static bool TryNormalizeOffer(
            IEnumerable<InventoryItemDto> items,
            out IReadOnlyList<InventoryItemDto> offer,
            out TradeOperationStatusEnum status)
        {
            offer = Array.Empty<InventoryItemDto>();
            status = TradeOperationStatusEnum.InvalidOffer;

            if (items == null)
            {
                return false;
            }

            var totals = new Dictionary<InventoryItemEnum, long>();
            var entryCount = 0;

            foreach (var item in items)
            {
                entryCount++;

                if (entryCount > MaximumOfferItemTypes)
                {
                    status = TradeOperationStatusEnum.OfferLimitReached;

                    return false;
                }

                if (item == null
                    || !Enum.IsDefined(typeof(InventoryItemEnum), item.Type)
                    || item.Type == InventoryItemEnum.None
                    || item.Count <= 0)
                {
                    return false;
                }

                totals.TryGetValue(item.Type, out var total);
                total += item.Count;

                if (total > int.MaxValue)
                {
                    return false;
                }

                totals[item.Type] = total;
            }

            if (totals.Count > MaximumOfferItemTypes)
            {
                status = TradeOperationStatusEnum.OfferLimitReached;

                return false;
            }

            offer = totals
                .OrderBy(x => x.Key)
                .Select(x => new InventoryItemDto { Type = x.Key, Count = (int)x.Value })
                .ToArray();
            status = TradeOperationStatusEnum.Applied;

            return true;
        }

        private static InventoryItemDto[] CloneItems(IEnumerable<InventoryItemDto> items)
        {
            return items
                .Select(x => new InventoryItemDto { Type = x.Type, Count = x.Count })
                .ToArray();
        }

        private static void RemoveSession(TradeSession session)
        {
            _sessionsByPlayer.Remove(session.FirstClientId);
            _sessionsByPlayer.Remove(session.SecondClientId);
        }

        private static void CompleteInventoryMutation(ulong clientId)
        {
            if (!_inventoryMutationsByPlayer.TryGetValue(clientId, out var count) || count <= 1)
            {
                _inventoryMutationsByPlayer.Remove(clientId);

                return;
            }

            _inventoryMutationsByPlayer[clientId] = count - 1;
        }

        private static bool RemoveInvitationsFor(ulong clientId)
        {
            var removed = _invitersByTarget.Remove(clientId);

            foreach (var targetClientId in _invitersByTarget
                .Where(x => x.Value == clientId)
                .Select(x => x.Key)
                .ToArray())
            {
                _invitersByTarget.Remove(targetClientId);
                removed = true;
            }

            return removed;
        }

        public sealed class TradeSession
        {
            public TradeSession(Guid id, ulong firstClientId, ulong secondClientId)
            {
                Id = id;
                FirstClientId = firstClientId;
                SecondClientId = secondClientId;
            }

            public Guid Id { get; }

            public ulong FirstClientId { get; }

            public ulong SecondClientId { get; }

            public IReadOnlyList<InventoryItemDto> FirstOffer { get; private set; } = Array.Empty<InventoryItemDto>();

            public IReadOnlyList<InventoryItemDto> SecondOffer { get; private set; } = Array.Empty<InventoryItemDto>();

            public bool FirstLocked { get; set; }

            public bool SecondLocked { get; set; }

            public bool FirstConfirmed { get; set; }

            public bool SecondConfirmed { get; set; }

            public bool IsCommitting { get; set; }

            public ulong GetPartner(ulong clientId)
            {
                return clientId == FirstClientId ? SecondClientId : FirstClientId;
            }

            public IReadOnlyList<InventoryItemDto> GetOffer(ulong clientId)
            {
                return clientId == FirstClientId ? FirstOffer : SecondOffer;
            }

            public bool IsLocked(ulong clientId)
            {
                return clientId == FirstClientId ? FirstLocked : SecondLocked;
            }

            public bool IsConfirmed(ulong clientId)
            {
                return clientId == FirstClientId ? FirstConfirmed : SecondConfirmed;
            }

            public void SetOffer(ulong clientId, IReadOnlyList<InventoryItemDto> offer)
            {
                if (clientId == FirstClientId)
                {
                    FirstOffer = offer;
                }
                else
                {
                    SecondOffer = offer;
                }
            }

            public void SetLocked(ulong clientId, bool locked)
            {
                if (clientId == FirstClientId)
                {
                    FirstLocked = locked;
                }
                else
                {
                    SecondLocked = locked;
                }
            }

            public void SetConfirmed(ulong clientId, bool confirmed)
            {
                if (clientId == FirstClientId)
                {
                    FirstConfirmed = confirmed;
                }
                else
                {
                    SecondConfirmed = confirmed;
                }
            }
        }

        public sealed class TradeCommit
        {
            public TradeCommit(
                Guid sessionId,
                ulong firstClientId,
                ulong secondClientId,
                InventoryItemDto[] firstOffer,
                InventoryItemDto[] secondOffer)
            {
                SessionId = sessionId;
                FirstClientId = firstClientId;
                SecondClientId = secondClientId;
                FirstOffer = firstOffer;
                SecondOffer = secondOffer;
            }

            public Guid SessionId { get; }

            public ulong FirstClientId { get; }

            public ulong SecondClientId { get; }

            public InventoryItemDto[] FirstOffer { get; }

            public InventoryItemDto[] SecondOffer { get; }
        }

        private sealed class InventoryMutationScope : IDisposable
        {
            private readonly ulong _clientId;
            private bool _disposed;

            public InventoryMutationScope(ulong clientId)
            {
                _clientId = clientId;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                CompleteInventoryMutation(_clientId);
            }
        }
    }
}
