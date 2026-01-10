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
            HealthSubscription.Instance.Subscribe(gameObject.GetInstanceID().ToString(), (e) =>
            {
                Network.Value -= e.Value;
                Debug.Log($"Object damaged. Damage: {e.Value}, CurrentValue: {Network.Value}");

                var targetSelectorSubscriptionsEvent = new TargetSelectorSubscriptionsEvent
                {
                    Key = gameObject.GetComponent<NetworkObject>().NetworkObjectId.ToString(),
                    Value = Network.Value
                };

                if (Network.Value <= 0)
                {
                    Debug.Log("Object killed");

                    targetSelectorSubscriptionsEvent.Hide = true;

                    TargetSelectorSubscription.Instance.Invoke(new TargetSelectorSubscriptionsEvent
                    {
                        Key = gameObject.GetInstanceID().ToString(),
                        Hide = true
                    });

                    ReleaseSubscription.Instance.Invoke(new ReleaseSubscriptionEvent
                    {
                        Key = gameObject.GetInstanceID().ToString(),
                    });

                    QuestSubscription.Instance.Invoke(new QuestSubscriptionEvent
                    {
                        Key = e.ClientId.ToString(),
                        ClientToken = e.ClientToken,
                        GameObjectName = gameObject.name
                    });

                    InventorySubscription.Instance.Invoke(new InventorySubscriptionEvent
                    {
                        Key = e.ClientId.ToString(),
                        ClientToken = e.ClientToken,
                    });

                    ExperienceSubscription.Instance.Invoke(new ExperienceSubscriptionEvent
                    {
                        Key = e.ClientId.ToString(),
                        ClientToken = e.ClientToken,
                    });
                }

                TargetSelectorSubscription.Instance.Invoke(targetSelectorSubscriptionsEvent);
            });
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                Network.Value = 100;
                HealthSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnNetworkDespawn();
        }
    }
}