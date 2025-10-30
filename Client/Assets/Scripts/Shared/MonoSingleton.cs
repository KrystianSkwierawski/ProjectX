using UnityEngine;

namespace Assets.Scripts.Shared
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : class
    {
        public static T Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }
}