using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private GameObject _playerCanvas;

    private void Start()
    {
        if (IsOwner)
        {
            _playerCanvas = GameObject.Find("PlayerCanvas");

            // todo: get from api
            var level = 1;
            var health = 100;

            _playerCanvas.transform.Find("Player/Name").GetComponent<TextMeshProUGUI>().text = ClientTokenManager.Instance.UserName.Split('@')[0];
            _playerCanvas.transform.Find("Player/HealthPoints").GetComponent<TextMeshProUGUI>().text = health.ToString();
            _playerCanvas.transform.Find("Player/Level").GetComponent<TextMeshProUGUI>().text = $"Level: {level}";
        }
    }
}
