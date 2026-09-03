using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Areas.Party.Enums;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Areas.Party.Mono
{
    internal static class PartyServerState
    {
        private static readonly IDictionary<ulong, PartyGroup> _partiesByMember = new Dictionary<ulong, PartyGroup>();
        private static readonly IDictionary<ulong, HashSet<ulong>> _invitersByTarget = new Dictionary<ulong, HashSet<ulong>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _partiesByMember.Clear();
            _invitersByTarget.Clear();
        }

        public static PartyOperationStatusEnum Invite(ulong inviterClientId, ulong targetClientId)
        {
            if (inviterClientId == targetClientId)
            {
                return PartyOperationStatusEnum.CannotInviteSelf;
            }

            if (_partiesByMember.ContainsKey(targetClientId))
            {
                return PartyOperationStatusEnum.TargetAlreadyInParty;
            }

            if (_partiesByMember.TryGetValue(inviterClientId, out var inviterParty) && inviterParty.LeaderClientId != inviterClientId)
            {
                return PartyOperationStatusEnum.OnlyLeaderCanInvite;
            }

            if (!_invitersByTarget.TryGetValue(targetClientId, out var inviters))
            {
                inviters = new HashSet<ulong>();
                _invitersByTarget[targetClientId] = inviters;
            }

            return inviters.Add(inviterClientId)
                ? PartyOperationStatusEnum.Applied
                : PartyOperationStatusEnum.InvitationAlreadyPending;
        }

        public static PartyOperationStatusEnum Respond(ulong targetClientId, ulong inviterClientId, bool accept)
        {
            if (!_invitersByTarget.TryGetValue(targetClientId, out var inviters) || !inviters.Remove(inviterClientId))
            {
                return PartyOperationStatusEnum.InvitationNotFound;
            }

            RemoveEmptyInvitationSet(targetClientId, inviters);

            if (!accept)
            {
                return PartyOperationStatusEnum.Applied;
            }

            if (_partiesByMember.ContainsKey(targetClientId))
            {
                return PartyOperationStatusEnum.TargetAlreadyInParty;
            }

            if (_partiesByMember.TryGetValue(inviterClientId, out var inviterParty))
            {
                if (inviterParty.LeaderClientId != inviterClientId)
                {
                    return PartyOperationStatusEnum.InvitationNotFound;
                }
            }
            else
            {
                inviterParty = new PartyGroup(inviterClientId);
                _partiesByMember[inviterClientId] = inviterParty;
            }

            inviterParty.Members.Add(targetClientId);
            _partiesByMember[targetClientId] = inviterParty;

            RemoveIncomingInvitations(targetClientId);
            RemoveOutgoingInvitations(targetClientId);

            return PartyOperationStatusEnum.Applied;
        }

        public static PartyOperationStatusEnum Leave(ulong clientId)
        {
            if (!_partiesByMember.TryGetValue(clientId, out var party))
            {
                return PartyOperationStatusEnum.PartyNotFound;
            }

            RemoveMember(clientId, party);

            return PartyOperationStatusEnum.Applied;
        }

        public static void RemovePlayer(ulong clientId)
        {
            if (_partiesByMember.TryGetValue(clientId, out var party))
            {
                RemoveMember(clientId, party);
            }

            RemoveIncomingInvitations(clientId);
            RemoveOutgoingInvitations(clientId);
        }

        public static bool TryGetParty(ulong clientId, out IReadOnlyList<ulong> members, out ulong leaderClientId)
        {
            if (!_partiesByMember.TryGetValue(clientId, out var party))
            {
                members = null;
                leaderClientId = default;

                return false;
            }

            members = party.Members;
            leaderClientId = party.LeaderClientId;

            return true;
        }

        public static IReadOnlyList<ulong> GetEligibleRewardMembers(
            ulong sourceClientId,
            Vector3 sourcePosition,
            float maxDistance)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || maxDistance < 0f)
            {
                return System.Array.Empty<ulong>();
            }

            var candidates = _partiesByMember.TryGetValue(sourceClientId, out var party)
                ? party.Members
                : new List<ulong> { sourceClientId };

            var maxDistanceSquared = maxDistance * maxDistance;
            var eligibleMembers = new List<ulong>();

            foreach (var clientId in candidates)
            {
                if (clientId == sourceClientId)
                {
                    eligibleMembers.Add(clientId);

                    Debug.Log($"Party reward eligible. SourceClientId: {sourceClientId}, MemberClientId: {clientId}, Reason: Killer.");

                    continue;
                }

                if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                {
                    Debug.Log($"Party reward ineligible. SourceClientId: {sourceClientId}, MemberClientId: {clientId}, Reason: Disconnected.");

                    continue;
                }

                if (client.PlayerObject == null)
                {
                    Debug.Log($"Party reward ineligible. SourceClientId: {sourceClientId}, MemberClientId: {clientId}, Reason: MissingPlayerObject.");

                    continue;
                }

                var distanceSquared = (client.PlayerObject.transform.position - sourcePosition).sqrMagnitude;
                var distance = Mathf.Sqrt(distanceSquared);

                if (distanceSquared > maxDistanceSquared)
                {
                    Debug.Log($"Party reward ineligible. SourceClientId: {sourceClientId}, MemberClientId: {clientId}, Distance: {distance:F2}, MaxDistance: {maxDistance:F2}, Reason: TooFar.");

                    continue;
                }

                eligibleMembers.Add(clientId);

                Debug.Log($"Party reward eligible. SourceClientId: {sourceClientId}, MemberClientId: {clientId}, Distance: {distance:F2}, MaxDistance: {maxDistance:F2}.");
            }

            return eligibleMembers;
        }

        public static IReadOnlyList<ulong> GetInviters(ulong targetClientId)
        {
            return _invitersByTarget.TryGetValue(targetClientId, out var inviters)
                ? inviters.ToArray()
                : System.Array.Empty<ulong>();
        }

        private static void RemoveMember(ulong clientId, PartyGroup party)
        {
            party.Members.Remove(clientId);
            _partiesByMember.Remove(clientId);
            RemoveOutgoingInvitations(clientId);

            if (party.Members.Count == 0)
            {
                return;
            }

            if (party.LeaderClientId == clientId)
            {
                party.LeaderClientId = party.Members[0];
            }
        }

        private static void RemoveIncomingInvitations(ulong targetClientId)
        {
            _invitersByTarget.Remove(targetClientId);
        }

        private static void RemoveOutgoingInvitations(ulong inviterClientId)
        {
            foreach (var targetClientId in _invitersByTarget.Keys.ToArray())
            {
                var inviters = _invitersByTarget[targetClientId];

                inviters.Remove(inviterClientId);
                RemoveEmptyInvitationSet(targetClientId, inviters);
            }
        }

        private static void RemoveEmptyInvitationSet(ulong targetClientId, IReadOnlyCollection<ulong> inviters)
        {
            if (inviters.Count == 0)
            {
                _invitersByTarget.Remove(targetClientId);
            }
        }

        private sealed class PartyGroup
        {
            public PartyGroup(ulong leaderClientId)
            {
                LeaderClientId = leaderClientId;
                Members.Add(leaderClientId);
            }

            public List<ulong> Members { get; } = new List<ulong>();

            public ulong LeaderClientId { get; set; }
        }
    }
}
