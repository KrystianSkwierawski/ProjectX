using System;
using System.Threading;
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
        private readonly SemaphoreSlim _saveGate = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _lifetime;
        private float _nextSave;
        private bool _ready;
        private bool _saving;

        public override void OnNetworkSpawn()
        {
            _lifetime = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _ready = false;
            _nextSave = Time.unscaledTime + 5f;

            if (IsOwner)
            {
                LoadAsync(_lifetime.Token).Forget();
            }
        }

        private async UniTask LoadAsync(CancellationToken token)
        {
            try
            {
                var characterId = UserManager.Instance.SelectedCharacterId;
                var result = await UnityWebRequestHelper.ExecuteGetAsync<CharacterTransformDto>(
                    $"CharacterTransforms?CharacterId={characterId}", cancellationToken: token);

                if (!IsCurrent(token))
                {
                    return;
                }

                if (result.SceneName != DungeonTravel.WorldScene)
                {
                    throw new InvalidOperationException($"Unsupported saved scene: {result.SceneName}.");
                }

                var position = new Vector3(result.PositionX, result.PositionY, result.PositionZ);
                var rotation = Quaternion.Euler(0, result.RotationY, 0);
                var controller = GetComponent<CharacterController>();
                controller.enabled = false;

                transform.SetPositionAndRotation(position, rotation);
                GetComponent<ClientNetworkTransform>().Teleport(position, rotation, transform.localScale);
                controller.enabled = true;
                ReadyServerRpc();

                Debug.Log($"Character location restored. CharacterId: {characterId}, Scene: {result.SceneName}.");
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        [ServerRpc]
        private void ReadyServerRpc()
        {
            _ready = true;
        }

        private void Update()
        {
            if (IsServer && IsSpawned && _ready && !_saving && Time.unscaledTime >= _nextSave
                && !GetComponent<DungeonTravel>().IsTransitioning)
            {
                _nextSave = Time.unscaledTime + 5f;
                SavePeriodicAsync().Forget();
            }
        }

        private async UniTask SavePeriodicAsync()
        {
            _saving = true;

            try
            {
                await SaveTransformAsync(GetComponent<DungeonTravel>().PersistenceTransform);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _saving = false;
            }
        }

        public async UniTask SaveTransformAsync(Pose pose)
        {
            var token = _lifetime.Token;
            await _saveGate.WaitAsync(token);

            try
            {
                if (!IsCurrent(token))
                {
                    return;
                }

                var playerSessionId = UserManager.Instance.GetPlayerSessionId(OwnerClientId);

                await UnityWebRequestHelper.ExecutePostAsync<EmptyResponse>("CharacterTransforms", new CharacterTransformDto
                {
                    SceneName = DungeonTravel.WorldScene,
                    PositionX = pose.position.x,
                    PositionY = pose.position.y,
                    PositionZ = pose.position.z,
                    RotationY = pose.rotation.eulerAngles.y,
                }, playerSessionId, log: false, cancellationToken: token);
            }
            finally
            {
                _saveGate.Release();
            }
        }

        private bool IsCurrent(CancellationToken token)
        {
            return !token.IsCancellationRequested && IsSpawned && _lifetime?.Token == token;
        }

        public override void OnNetworkDespawn()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _lifetime = null;

            base.OnNetworkDespawn();
        }
    }
}
