using Assets.Scripts.Areas.Character.Mono;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Areas.Shared.Mono
{
    // Visibility is installed before Spawn, including for pooled objects and late joiners.
    public sealed class DungeonWorldObject : MonoBehaviour
    {
        public int InstanceId { get; set; }

        private NetworkObject _networkObject;
        private bool _followsOwner;

        public int CurrentInstanceId => _followsOwner
            ? DungeonTravel.GetInstanceId(_networkObject.OwnerClientId)
            : InstanceId;

        private void Awake()
        {
            _networkObject = GetComponent<NetworkObject>();
            _followsOwner = GetComponentInChildren<DungeonTravel>(true) != null || GetComponent<AbstractWeapon>() != null;

            _networkObject.CheckObjectVisibility = clientId =>
                (_networkObject.IsPlayerObject && clientId == _networkObject.OwnerClientId)
                || CurrentInstanceId == DungeonTravel.GetInstanceId(clientId);
        }

        public static bool SameInstance(GameObject first, GameObject second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            return GetInstanceId(first) == GetInstanceId(second);
        }

        private static int GetInstanceId(GameObject value)
        {
            return value.GetComponentInParent<DungeonWorldObject>()?.CurrentInstanceId ?? 0;
        }
    }
}
