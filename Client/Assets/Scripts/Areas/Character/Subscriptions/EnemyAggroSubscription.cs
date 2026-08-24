using UnityEngine;
using Assets.Scripts.Areas.Shared.Subscriptions;

namespace Assets.Scripts.Areas.Character.Subscriptions
{
    public class EnemyAggroSubscription : AbstractSubscription<EnemyAggroSubscription, EnemyAggroSubscriptionEvent>
    {
    }

    public class EnemyAggroSubscriptionEvent
    {
        public ulong ClientId { get; set; }

        public GameObject Target { get; set; }

        public string PlayerSessionId { get; set; }
    }
}
