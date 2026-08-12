using Assets.Scripts.Areas.Character.Models;
using Assets.Scripts.Areas.Shared.Models;
using Assets.Scripts.Areas.Shared.Mono;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Areas.Character.Mono
{
    public class CharacterTransform : NetworkBehaviour
    {
        private float _period = 0.0f;
        private const float _saveInterval = 5f;

        public override async void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                var result = await UnityWebRequestHelper.ExecuteGetAsync<CharacterTransformDto>("CharacterTransforms");

                transform.position = new Vector3(result.PositionX, result.PositionY, result.PositionZ);
                transform.rotation.Set(0, result.RotationY, 0, 0);
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                CheckSaveTransform();
            }
        }

        private void CheckSaveTransform()
        {
            if (_period > _saveInterval)
            {
                SaveTransformServerRpc();

                _period = 0;
            }

            _period += Time.deltaTime;
        }

        [ServerRpc]
        private void SaveTransformServerRpc()
        {
            var playerSessionId = UserManager.Instance.GetPlayerSessionId(OwnerClientId);

            UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("CharacterTransforms", new CharacterTransformDto
            {
                PositionX = transform.position.x,
                PositionY = transform.position.y,
                PositionZ = transform.position.z,
                RotationY = transform.rotation.y,
            }, playerSessionId, log: false)
            .Forget();
        }
    }
}
