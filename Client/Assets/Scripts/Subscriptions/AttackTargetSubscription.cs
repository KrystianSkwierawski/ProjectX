using UnityEngine;

namespace Assets.Scripts.Shared
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
