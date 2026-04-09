using Assets.Scripts.Shared;
using UnityEngine;

namespace Assets.Scripts.Subscriptions
{
    public class EnemyAggroSubscription : AbstractSubscription<EnemyAggroSubscription, EnemyAggroSubscriptionEvent>
    {
    }

    public class EnemyAggroSubscriptionEvent
    {
        public ulong ClientId { get; set; }

        public GameObject Target { get; set; }
    }
}
