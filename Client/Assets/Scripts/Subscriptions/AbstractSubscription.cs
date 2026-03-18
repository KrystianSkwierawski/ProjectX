using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Shared
{
    public abstract class AbstractSubscription<T, J> : Singleton<T>
        where T : AbstractSubscription<T, J>, new()
        where J : class

    {
        protected readonly IDictionary<string, UnityAction<J>> Subscriptions = new Dictionary<string, UnityAction<J>>();

        public void Subscribe(int key, UnityAction<J> action)
        {
            Subscribe(key.ToString(), action);
        }

        public virtual void Subscribe(string key, UnityAction<J> action)
        {
            if (!Subscriptions.ContainsKey(key))
            {
                Debug.Log($"Subscribe -> Type: {typeof(J)}, Key: {key}");

                Subscriptions.Add(key, action);
            }
        }

        public virtual void Unsubscribe(string key)
        {
            if (Subscriptions.ContainsKey(key))
            {
                Debug.Log($"Unsubscribe -> Type: {typeof(J)}, Key: {key}");

                Subscriptions.Remove(key);
            }
        }

        public virtual void UnsubscribeAll()
        {
            if (Subscriptions.Any())
            {
                Debug.Log($"UnsubscribeAll -> Type: {typeof(J)}");

                Subscriptions.Clear();
            }
        }

        public virtual void Invoke(string key, J e)
        {
            if (Subscriptions.TryGetValue(key, out var action))
            {
                Debug.Log($"Invoke -> Type: {typeof(J)}, Key: {key}");
                action.Invoke(e);
            }
        }

        public virtual void InvokeAndUnsubscribe(string key, J e)
        {
            Invoke(key, e);
            Unsubscribe(key);
        }
    }
}
