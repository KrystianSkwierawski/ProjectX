using UnityEngine;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
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
		
		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}
    }
}