using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField] private UIManager _uiManager;

    private async void Start()
    {
        if (IsOwner)
        {
            await GetCharacterAsync();
        }
    }

    private async UniTask GetCharacterAsync()
    {
        var result = await UnityWebRequestHelper.ExecuteGetAsync<CharacterDto>("Characters/1");

        UIManager.Instance.SetPlayer(result.name, result.health.ToString(), result.level.ToString());
    }
}
