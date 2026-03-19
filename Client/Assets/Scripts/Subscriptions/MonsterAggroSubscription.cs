using Assets.Scripts.Shared;
using UnityEngine;

namespace Assets.Scripts.Subscriptions
{
    public class MonsterAggroSubscription : AbstractSubscription<MonsterAggroSubscription, MonsterAggroSubscriptionEvent>
    {
    }

    public class MonsterAggroSubscriptionEvent
    {
        public ulong ClientId { get; set; }

        public GameObject Target { get; set; }
    }
}
