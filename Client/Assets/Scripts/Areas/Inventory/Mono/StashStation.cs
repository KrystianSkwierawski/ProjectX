using UnityEngine;

namespace Assets.Scripts.Areas.Inventory.Mono
{
    public sealed class StashStation : MonoBehaviour
    {
        public const float InteractionDistance = 5f;

        public static StashStation Instance { get; private set; }

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool IsInRange(Transform player)
        {
            return Vector3.Distance(player.position, transform.position) <= InteractionDistance;
        }
    }
}
