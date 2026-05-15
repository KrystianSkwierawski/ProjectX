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
        protected IList<(string, UnityAction<J>)> Subscriptions = new List<(string, UnityAction<J>)>();

        public void Subscribe(int key, UnityAction<J> action)
        {
            Subscribe(key.ToString(), action);
        }

        public virtual void Subscribe(string key, UnityAction<J> action)
        {
            Debug.Log($"Subscribe -> Type: {typeof(J)}, Key: {key}");

            Subscriptions.Add((key, action));
        }

        public virtual void Unsubscribe(string key)
        {
            Debug.Log($"Unsubscribe -> Type: {typeof(J)}, Key: {key}");

            Subscriptions = Subscriptions.Where(x => x.Item1 != key).ToList();
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
            Debug.Log($"Invoke -> Type: {typeof(J)}, Key: {key}");

            var actions = Subscriptions.Where(x => x.Item1 == key);

            foreach (var action in actions)
            {
                action.Item2.Invoke(e);
            }
        }

        public virtual void InvokeAndUnsubscribe(string key, J e)
        {
            Invoke(key, e);
            Unsubscribe(key);
        }
    }
}
