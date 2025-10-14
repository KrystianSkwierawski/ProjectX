using VContainer;
using VContainer.Unity;

public class CharacterLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<CharacterPlayerController>().As<IStartable>().As<ITickable>();
        builder.RegisterComponentInHierarchy<MusicPlayerController>().As<ITickable>();
    }
}