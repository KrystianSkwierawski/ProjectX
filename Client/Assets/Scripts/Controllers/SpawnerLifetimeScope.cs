using VContainer;
using VContainer.Unity;

public class SpawnerLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<SpawnerController>().As<ITickable>();
    }
}
