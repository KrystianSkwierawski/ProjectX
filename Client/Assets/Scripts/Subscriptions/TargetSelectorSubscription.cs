using System.Linq;
using Assets.Scripts.Shared;
using UnityEngine;

namespace Assets.Scripts.Subscriptions
{
    public class TargetSelectorSubscription : AbstractSubscription<TargetSelectorSubscription, TargetSelectorSubscriptionsEvent>
    {
        public override void Invoke(string key, TargetSelectorSubscriptionsEvent e)
        {
            foreach (var subscription in Subscriptions.Where(x => x.Key.StartsWith($"{key}_")))
            {
                Debug.Log($"Invoke -> Type: TargetSelectorSubscriptionsEvent, Id: Key: {subscription.Key}");

                subscription.Value.Invoke(e);
            }
        }
    }

    public class TargetSelectorSubscriptionsEvent
    {
        public bool Hide { get; set; }

        public float Value { get; set; }
    }
}
