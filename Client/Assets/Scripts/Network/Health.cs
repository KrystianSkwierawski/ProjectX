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
                    Value = Network.Value
                };

                if (Network.Value <= 0)
                {
                    Debug.Log("Object killed");

                    targetSelectorSubscriptionsEvent.Hide = true;

                    ReleaseSubscription.Instance.Invoke(gameObject.GetInstanceID().ToString(), new ReleaseSubscriptionEvent());

                    QuestSubscription.Instance.Invoke(e.ClientId.ToString(), new QuestSubscriptionEvent
                    {
                        ClientToken = e.ClientToken,
                        GameObjectName = gameObject.name
                    });

                    InventorySubscription.Instance.Invoke(e.ClientId.ToString(), new InventorySubscriptionEvent
                    {
                        ClientToken = e.ClientToken,
                    });

                    ExperienceSubscription.Instance.Invoke(e.ClientId.ToString(), new ExperienceSubscriptionEvent
                    {
                        ClientToken = e.ClientToken,
                    });
                }

                TargetSelectorSubscription.Instance.Invoke(gameObject.GetComponent<NetworkObject>().NetworkObjectId.ToString(), targetSelectorSubscriptionsEvent);
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