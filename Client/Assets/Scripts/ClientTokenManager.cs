using UnityEngine;

public class ClientTokenManager : BaseTokenManager
{
    public static ClientTokenManager Instance { get; private set; }

#if UNITY_EDITOR
    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // FIXME: tmp login simulation
        if (Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
        {
            UserName = "user1@localhost";
            Password = "User1!";
            Debug.Log("Login player1");
        }
        else
        {
            UserName = "user2@localhost";
            Password = "User2!";
            Debug.Log("Login player2");
        }

        await LoginAsync();
    }
#endif
}
