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
            UpdateHealthSubscription.Instance.Subscribe(gameObject.GetInstanceID().ToString(), (e) =>
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

                    targetSelectorSubscriptionsEvent.Hide = true;

                    ReleasePoolSubscription.Instance.Invoke(gameObject.GetInstanceID().ToString(), new ReleasePoolSubscriptionEvent());

                    CheckCharacterQuestSubscription.Instance.Invoke(e.ClientId.ToString(), new CheckCharacterQuestSubscriptionEvent
                    {
                        Progress = 1,
                        GameObjectName = gameObject.name,
                        ClientToken = e.ClientToken,
                    });

                    UpdateInventorySubscription.Instance.Invoke(e.ClientId.ToString(), new UpdateInventorySubscriptionEvent
                    {
                        ClientToken = e.ClientToken,
                    });

                    AddExperienceSubscription.Instance.Invoke(e.ClientId.ToString(), new AddExperienceSubscriptionEvent
                    {
                        ClientToken = e.ClientToken,
                    });
                }

                UpdateTargetSelectorSubscription.Instance.Invoke(gameObject.GetComponent<NetworkObject>().NetworkObjectId.ToString(), targetSelectorSubscriptionsEvent);
            });
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                Network.Value = 100;
                UpdateHealthSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            if (IsServer)
            {
                UpdateHealthSubscription.Instance.Unsubscribe(OwnerClientId.ToString());
            }

            base.OnDestroy();
        }
    }
}