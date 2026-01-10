using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Shared
{
    public abstract class AbstractSubscription<T, J> : Singleton<T>
        where T : AbstractSubscription<T, J>, new()
        where J : ISubscriptionEvent

    {
        protected readonly IDictionary<string, UnityAction<J>> Subscriptions = new Dictionary<string, UnityAction<J>>();

        public virtual void Subscribe(string key, UnityAction<J> action)
        {
            Debug.Log($"Subscribe -> Type: {typeof(J)}, Key: {key}");

            Subscriptions.Add(key, action);
        }

        public virtual void Unsubscribe(string key)
        {
            Debug.Log($"Unsubscribe -> Type: {typeof(J)}, Key: {key}");

            if (Subscriptions.ContainsKey(key))
            {
                Subscriptions.Remove(key);
            }
        }

        public virtual void UnsubscribeAll()
        {
            Debug.Log($"UnsubscribeAll -> Type: {typeof(J)}");

            Subscriptions.Clear();
        }

        public virtual void Invoke(J e)
        {
            Debug.Log($"Invoke -> Type: {typeof(J)}, Id: {e.Key}");

            if (Subscriptions.TryGetValue(e.Key, out var action))
            {
                action.Invoke(e);
            }
        }
    }

    public interface ISubscriptionEvent
    {
        string Key { get; set; }
    }
}
