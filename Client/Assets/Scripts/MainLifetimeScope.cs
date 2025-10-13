using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainLifetimeScope : LifetimeScope
{
    [SerializeField] private CharacterController _characterController;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ITokenManagerService, TokenManagerService>(Lifetime.Singleton);
        builder.Register<ICharacterService, CharacterTransformService>(Lifetime.Singleton);

#if UNITY_EDITOR
        builder.RegisterEntryPoint<MainClientController>()
            .As<IStartable>();
#elif UNITY_SERVER && !UNITY_EDITOR
        builder.RegisterEntryPoint<MainServerController>()
            .As<IStartable>();
#endif
    }
}

public class MainClientController : IStartable
{
    private readonly ITokenManagerService _tokenManagerService;

    public MainClientController(ITokenManagerService tokenManagerService)
    {
        _tokenManagerService = tokenManagerService;
    }

    public async void Start()
    {
        if (Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
        {
            await _tokenManagerService.LoginAsync("user1@localhost", "User1!");
        }
        else
        {
            await _tokenManagerService.LoginAsync("user2@localhost", "User2!");
        }

        NetworkManager.Singleton.StartClient();
    }
}

public class MainServerController : IStartable
{
    private readonly ITokenManagerService _tokenManagerService;

    public MainServerController(ITokenManagerService tokenManagerService)
    {
        _tokenManagerService = tokenManagerService;
    }

    public async void Start()
    {
        await _tokenManagerService.LoginAsync("server1@localhost", "Server1!");

        NetworkManager.Singleton.StartServer();
    }
}
