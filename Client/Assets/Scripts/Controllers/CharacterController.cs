using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CharacterController : NetworkBehaviour, IStartable, ITickable
{
    private float _timer;
    private const float SaveInterval = 5f;

    [Inject] private readonly ICharacterService _characterService;
    [Inject] private readonly ITokenManagerService _tokenManagerService;

    public async void Start()
    {
        if (IsOwner)
        {
            await UniTask.WhenAll(LoadInfoAsync(), LoadTransformAsync());
        }
    }

    public void Tick()
    {
        if (IsOwner)
        {
            Check();
        }
    }

    private void Check()
    {
        _timer += Time.deltaTime;

        if (_timer >= SaveInterval)
        {
            SaveTransformServerRpc(_tokenManagerService.Token);
            _timer = 0f;
        }
    }

    private async UniTask LoadInfoAsync()
    {
        var result = await _characterService.GetInfoAsync();

        var playerCanvas = GameObject.Find("PlayerCanvas/Player");
        playerCanvas.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = result.name;
        playerCanvas.transform.Find("HealthPoints").GetComponent<TextMeshProUGUI>().text = result.health.ToString();
        playerCanvas.transform.Find("Level").GetComponent<TextMeshProUGUI>().text = $"Level: {result.level}";
    }

    private async UniTask LoadTransformAsync()
    {
        var result = await _characterService.GetTransformAsync(_tokenManagerService.Token);

        transform.position = new Vector3(result.positionX, result.positionY, result.positionZ);
        transform.rotation = Quaternion.Euler(0, result.rotationY, 0);
    }

    [ServerRpc]
    private void SaveTransformServerRpc(string token)
    {
        _characterService.SaveTransformAsync(transform.position, transform.rotation.eulerAngles.y, token);
    }
}