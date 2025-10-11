using Unity.Netcode;
using UnityEngine;
using VContainer;

public class CharacterNetworkBehaviour : NetworkBehaviour
{
    [Inject] private readonly ICharacterService _characterService;
    [Inject] private readonly ITokenManagerService _tokenManagerService;

    private float period = 0.0f;
    private const float _saveInterval = 5f;
    private GameObject _playerCanvas;

    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            _playerCanvas = GameObject.Find("PlayerCanvas");

            await _characterService.GetTransformAsync(transform);
            await _characterService.GetDetailsAsync(_playerCanvas);
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
        if (period > _saveInterval)
        {
            SaveTransformServerRpc(_tokenManagerService.Token);
            period = 0;
        }

        period += Time.deltaTime;
    }

    [ServerRpc]
    private void SaveTransformServerRpc(string token)
    {
        _ = _characterService.SaveTransformAsync(transform, token);
    }
}