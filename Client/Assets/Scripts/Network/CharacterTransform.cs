using Assets.Scripts.Models;
using Assets.Scripts.Shared;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Network
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

                transform.position = new Vector3(result.positionX, result.positionY, result.positionZ);
                transform.rotation.Set(0, result.rotationY, 0, 0);
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
                SaveTransformServerRpc(TokenManager.Instance.Token);
                _period = 0;
            }

            _period += Time.deltaTime;
        }

        [ServerRpc]
        private void SaveTransformServerRpc(string clientToken)
        {
            _ = UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("CharacterTransforms", new CharacterTransformDto
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                positionZ = transform.position.z,
                rotationY = transform.rotation.y,
            }, clientToken);
        }
    }
}