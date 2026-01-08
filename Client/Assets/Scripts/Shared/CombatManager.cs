using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Shared
{
    public class CombatManager : Singleton<CombatManager>
    {
        public UnityEvent<KillEventModel> OnKillEvent = new UnityEvent<KillEventModel>();
    }

    public class KillEventModel 
    {         
        public ulong ClientId { get; set; }

        public string ClientToken { get; set; }

        public GameObject GameObject { get; set; }
    }
}
