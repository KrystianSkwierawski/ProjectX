using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Assets.Scripts.Areas.Character.Enums;
using Assets.Scripts.Areas.Character.Subscriptions;
using Assets.Scripts.Areas.Party.Mono;
using Assets.Scripts.Areas.Quest.Enums;
using Assets.Scripts.Areas.Quest.Subscriptions;
using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Character.Mono
{
    public class Health : NetworkBehaviour
    {
        private const int _experienceReward = 50;
        private const float _partyRewardMaxDistance = 1000f;

        public NetworkVariable<float> Network { get; private set; } = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private void Start()
        {
            if (IsServer)
            {
                var gameObjectKey = gameObject.GetInstanceID().ToString();

                AttackTargetSubscription.Instance.Subscribe(gameObjectKey, (e) =>
                {
                    if (!IsSpawned || Network.Value <= 0)
                    {
                        return;
                    }

                    Network.Value -= e.Value;

                    Debug.Log($"Object damaged. Damage: {e.Value}, CurrentValue: {Network.Value}");

                    var targetSelectorSubscriptionsEvent = new UpdateTargetSelectorSubscriptionsEvent
                    {
                        Value = Network.Value
                    };

                    if (Network.Value <= 0)
                    {
                        Debug.Log("Object killed");

                        targetSelectorSubscriptionsEvent.Killed = true;

                        var rewardRecipients = GetRewardRecipients(e.ClientId, e.Player, e.PlayerSessionId);

                        GrantKillRewards(rewardRecipients);
                    }

                    EnemyAggroSubscription.Instance.Invoke(gameObjectKey, new EnemyAggroSubscriptionEvent
                    {
                        ClientId = e.ClientId,
                        Target = targetSelectorSubscriptionsEvent.Killed ? null : e.Player,
                        PlayerSessionId = e.PlayerSessionId,
                    });

                    UpdateTargetSelectorSubscription.Instance.Invoke(gameObjectKey, targetSelectorSubscriptionsEvent);

                    if (targetSelectorSubscriptionsEvent.Killed)
                    {
                        ReleasePoolSubscription.Instance.Invoke(gameObjectKey, new ReleasePoolSubscriptionEvent());
                    }
                });

                SetHealthSubscription.Instance.Subscribe(gameObjectKey, (e) =>
                {
                    Network.Value = e.Value;

                    UpdateTargetSelectorSubscription.Instance.Invoke(gameObjectKey, new UpdateTargetSelectorSubscriptionsEvent
                    {
                        Value = e.Value
                    });
                });
            }
        }

        private IReadOnlyDictionary<ulong, string> GetRewardRecipients(
            ulong killerClientId,
            GameObject killer,
            string killerSessionId)
        {
            var recipients = new Dictionary<ulong, string>();

            if (killer == null)
            {
                Debug.LogWarning($"Kill rewards skipped because client {killerClientId} has no player object.");

                return recipients;
            }

            foreach (var clientId in PartyServerState.GetEligibleRewardMembers(
                killerClientId,
                transform.position,
                _partyRewardMaxDistance))
            {
                if (clientId == killerClientId && !string.IsNullOrWhiteSpace(killerSessionId))
                {
                    recipients[clientId] = killerSessionId;
                }
                else if (UserManager.Instance.TryGetPlayerSessionId(clientId, out var playerSessionId))
                {
                    recipients[clientId] = playerSessionId;
                }
                else
                {
                    Debug.LogWarning($"Party reward ineligible. SourceClientId: {killerClientId}, MemberClientId: {clientId}, Reason: MissingPlayerSession.");
                }
            }

            return recipients;
        }

        private void GrantKillRewards(IReadOnlyDictionary<ulong, string> recipients)
        {
            if (recipients.Count == 0)
            {
                return;
            }

            var experiencePerMember = _experienceReward / recipients.Count;

            foreach (var recipient in recipients)
            {
                CheckCharacterQuestSubscription.Instance.Invoke(recipient.Key.ToString(), new CheckCharacterQuestSubscriptionEvent
                {
                    Progress = 1,
                    QuestType = QuestTypeEnum.Kill,
                    GameObjectName = gameObject.name,
                    PlayerSessionId = recipient.Value,
                });

                CheckLootSubscription.Instance.Invoke(recipient.Key.ToString(), new CheckLootSubscriptionEvent
                {
                    GameObjectName = gameObject.name
                });

                if (experiencePerMember > 0)
                {
                    AddExperienceSubscription.Instance.Invoke(recipient.Key.ToString(), new AddExperienceSubscriptionEvent
                    {
                        Amount = experiencePerMember,
                        Type = ExperienceTypeEnum.Main,
                        PlayerSessionId = recipient.Value,
                    });
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                Network.Value = 100;
            }

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            if (IsServer)
            {
                var key = gameObject.GetInstanceID().ToString();

                AttackTargetSubscription.Instance.Unsubscribe(key);
                SetHealthSubscription.Instance.Unsubscribe(key);
            }

            base.OnDestroy();
        }
    }
}
