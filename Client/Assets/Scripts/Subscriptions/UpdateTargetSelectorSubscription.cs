using System.Linq;
using Assets.Scripts.Shared;
using UnityEngine;

namespace Assets.Scripts.Subscriptions
{
    public class UpdateTargetSelectorSubscription : AbstractSubscription<UpdateTargetSelectorSubscription, UpdateTargetSelectorSubscriptionsEvent>
    {
        public override void Invoke(string key, UpdateTargetSelectorSubscriptionsEvent e)
        {
            foreach (var subscription in Subscriptions.Where(x => x.Item1.StartsWith($"{key}_")))
            {
                Debug.Log($"Invoke -> Type: TargetSelectorSubscriptionsEvent, Id: Key: {subscription.Item1}");

                subscription.Item2.Invoke(e);
            }
        }
    }

    public class UpdateTargetSelectorSubscriptionsEvent
    {
        public bool Killed { get; set; }

        public float Value { get; set; }
    }
}
