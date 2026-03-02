using Assets.Scripts.Shared;
using UnityEngine;

namespace Assets.Scripts.Subscriptions
{
    public class AttackPlayerSubscription : AbstractSubscription<AttackPlayerSubscription, PlayerAttackSubscriptionEvent>
    {
    }

    public class PlayerAttackSubscriptionEvent
    {
        public int Value { get; set; }

        public GameObject Monster { get; set; }
    }
}
