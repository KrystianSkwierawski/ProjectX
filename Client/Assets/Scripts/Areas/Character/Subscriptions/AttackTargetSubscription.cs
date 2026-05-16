using UnityEngine;
using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Character.Subscriptions
{
    public class AttackTargetSubscription : AbstractSubscription<AttackTargetSubscription, AttackTargetSubscriptionEvent>
    {
    }

    public class AttackTargetSubscriptionEvent
    {
        public ulong ClientId { get; set; }

        public float Value { get; set; }

        public string ClientToken { get; set; }

        public GameObject Player { get; set; }
    }
}
