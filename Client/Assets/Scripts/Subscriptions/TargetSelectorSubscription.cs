using System.Linq;
using Assets.Scripts.Shared;
using UnityEngine;

namespace Assets.Scripts.Subscriptions
{
    public class TargetSelectorSubscription : AbstractSubscription<TargetSelectorSubscription, TargetSelectorSubscriptionsEvent>
    {
        public override void Invoke(TargetSelectorSubscriptionsEvent e)
        {
            foreach (var subscription in Subscriptions.Where(x => x.Key.StartsWith($"{e.Key}_")))
            {
                Debug.Log($"Invoke -> Type: TargetSelectorSubscriptionsEvent, Id: Key: {subscription.Key}");

                subscription.Value.Invoke(e);
            }
        }
    }

    public class TargetSelectorSubscriptionsEvent : ISubscriptionEvent
    {
        public string Key { get; set; }

        public bool Hide { get; set; }

        public float Value { get; set; }
    }
}
