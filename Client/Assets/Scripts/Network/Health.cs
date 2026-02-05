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

                UpdateHealthSubscription.Instance.Subscribe(gameObjectKey, (e) =>
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
                            ClientToken = e.ClientToken,
                            GameObjectName = gameObject.name
                        });

                        AddExperienceSubscription.Instance.Invoke(e.ClientId.ToString(), new AddExperienceSubscriptionEvent
                        {
                            ClientToken = e.ClientToken,
                        });
                    }

                    UpdateTargetSelectorSubscription.Instance.Invoke(gameObjectKey, targetSelectorSubscriptionsEvent);
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
                UpdateHealthSubscription.Instance.Unsubscribe(gameObject.GetInstanceID().ToString());
            }

            base.OnDestroy();
        }
    }
}