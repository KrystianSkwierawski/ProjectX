using UnityEngine;
using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Character.Subscriptions
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
