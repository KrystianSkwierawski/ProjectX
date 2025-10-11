using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ITokenManagerService, TokenManagerService>(Lifetime.Singleton);
        builder.Register<ICharacterService, CharacterService>(Lifetime.Scoped);
        builder.Register<IExperienceService, ExperienceService>(Lifetime.Scoped);

#if UNITY_EDITOR
        builder.RegisterEntryPoint<MainClientController>();
#elif UNITY_SERVER && !UNITY_EDITOR
        builder.RegisterEntryPoint<MainServerController>();
#endif
    }
}

public class MainClientController : IAsyncStartable
{
    private readonly ITokenManagerService _tokenManagerService;

    public MainClientController(ITokenManagerService tokenManagerService)
    {
        _tokenManagerService = tokenManagerService;
    }

    public async UniTask StartAsync(CancellationToken cancellationToken)
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("Client started");

        // FIXME: tmp login simulation
        if (Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
        {
            await _tokenManagerService.LoginAsync("user1@localhost", "User1!");
        }
        else
        {
            await _tokenManagerService.LoginAsync("user2@localhost", "User2!");
        }
    }
}

public class MainServerController : IAsyncStartable
{
    private readonly ITokenManagerService _tokenManagerService;

    public MainServerController(ITokenManagerService tokenManagerService)
    {
        _tokenManagerService = tokenManagerService;
    }

    public async UniTask StartAsync(CancellationToken cancellationToken)
    {
        NetworkManager.Singleton.StartServer();
        Debug.Log("Server started");

        await _tokenManagerService.LoginAsync("server1@localhost", "Server1!");
    }
}
