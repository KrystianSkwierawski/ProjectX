using Assets.Scripts.Enums;
using Assets.Scripts.Shared;
using Assets.Scripts.Subscriptions;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Network
{
    public class Health : NetworkBehaviour
    {
        public NetworkVariable<float> Network { get; private set; } = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private void Start()
        {
            if (IsServer)
            {
                var gameObjectKey = gameObject.GetInstanceID().ToString();

                AttackTargetSubscription.Instance.Subscribe(gameObjectKey, (e) =>
                {
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

                        ReleasePoolSubscription.Instance.Invoke(gameObjectKey, new ReleasePoolSubscriptionEvent());

                        CheckCharacterQuestSubscription.Instance.Invoke(e.ClientId.ToString(), new CheckCharacterQuestSubscriptionEvent
                        {
                            Progress = 1,
                            GameObjectName = gameObject.name,
                            ClientToken = e.ClientToken,
                        });

                        CheckLootSubscription.Instance.Invoke(e.ClientId.ToString(), new CheckLootSubscriptionEvent
                        {
                            GameObjectName = gameObject.name
                        });

                        AddExperienceSubscription.Instance.Invoke(e.ClientId.ToString(), new AddExperienceSubscriptionEvent
                        {
                            Amount = 50,
                            Type = ExperienceTypeEnum.Main,
                            ClientToken = e.ClientToken,
                        });
                    }

                    EnemyAggroSubscription.Instance.Invoke(gameObjectKey, new EnemyAggroSubscriptionEvent
                    {
                        ClientId = e.ClientId,
                        Target = targetSelectorSubscriptionsEvent.Killed ? null : e.Player
                    });

                    UpdateTargetSelectorSubscription.Instance.Invoke(gameObjectKey, targetSelectorSubscriptionsEvent);
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