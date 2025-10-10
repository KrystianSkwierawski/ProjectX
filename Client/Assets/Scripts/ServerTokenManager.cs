public class ServerTokenManager : BaseTokenManager
{
    public static ServerTokenManager Instance { get; private set; }

#if UNITY_SERVER && !UNITY_EDITOR
    private async void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        UserName = "server1@localhost";
        Password = "Server1!";
        await LoginAsync();
    }
#endif
}
