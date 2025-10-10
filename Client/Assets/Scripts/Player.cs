using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

public class Player : NetworkBehaviour
{
    private GameObject _playerCanvas;
    public AudioClip BackgroundMusic;

    private void Start()
    {
        if (IsOwner)
        {
            GetComponent<AudioSource>().PlayOneShot(BackgroundMusic, 0.05f);
            _playerCanvas = GameObject.Find("PlayerCanvas");
            StartCoroutine(GetCharacter());
        }
    }

    private IEnumerator GetCharacter()
    {
        using var request = UnityWebRequest.Get("https://localhost:5001/api/Characters/1");

        request.SetRequestHeader("Authorization", $"Bearer {ClientTokenManager.Instance.Token}");

        yield return request.SendWebRequest();

        Debug.Log($"GetCharacter result: {request.result}");
        Debug.Log($"GetCharacter text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<CharacterDto>(request.downloadHandler.text);

            _playerCanvas.transform.Find("Player/Name").GetComponent<TextMeshProUGUI>().text = result.name;
            _playerCanvas.transform.Find("Player/HealthPoints").GetComponent<TextMeshProUGUI>().text = result.health.ToString();
            _playerCanvas.transform.Find("Player/Level").GetComponent<TextMeshProUGUI>().text = $"Level: {result.level}";
        }
    }

    private class CharacterDto
    {
        public string name;

        public byte level;

        public byte skillPoints;

        public int health;
    }
}
