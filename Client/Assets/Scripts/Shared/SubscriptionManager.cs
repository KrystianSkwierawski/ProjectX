using System.Collections.Generic;
using UnityEngine.Events;

namespace Assets.Scripts.Shared
{
    public class SubscriptionManager : Singleton<SubscriptionManager>
    {
        private readonly IDictionary<ulong, IList<UnityAction<KillActionEvent>>> _killSubscriptions = new Dictionary<ulong, IList<UnityAction<KillActionEvent>>>();
        private readonly IDictionary<int, UnityAction<ReleaseActionEvent>> _releaseSubscriptions = new Dictionary<int, UnityAction<ReleaseActionEvent>>();

        public void Subscribe(ulong clientId, UnityAction<KillActionEvent> action)
        {
            if (_killSubscriptions.TryGetValue(clientId, out var actions))
            {
                actions.Add(action);

                return;
            }


            _killSubscriptions.Add(clientId, new List<UnityAction<KillActionEvent>>() { action });
        }

        public void Subscribe(int instanceID, UnityAction<ReleaseActionEvent> action)
        {
            _releaseSubscriptions.Add(instanceID, action);
        }

        public void Invoke(KillActionEvent e)
        {
            if (_killSubscriptions.TryGetValue(e.ClientId, out var actions))
            {
                foreach (var action in actions)
                {
                    action.Invoke(e);
                }
            }
        }

        public void Invoke(ReleaseActionEvent e)
        {
            if (_releaseSubscriptions.TryGetValue(e.InstanceID, out var action))
            {
                action.Invoke(e);
            }
        }
    }

    public class KillActionEvent
    {
        public ulong ClientId { get; set; }

        public string ClientToken { get; set; }

        public string GameObjectName { get; set; }
    }

    public class ReleaseActionEvent
    {
        public int InstanceID { get; set; }
    }
}
