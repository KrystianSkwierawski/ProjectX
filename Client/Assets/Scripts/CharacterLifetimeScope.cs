using UnityEngine;
using VContainer;
using VContainer.Unity;

public class CharacterLifetimeScope : LifetimeScope
{
    [SerializeField] private CharacterController _characterController;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponent(_characterController).As<IStartable>().As<ITickable>();

        //builder.RegisterEntryPoint<CharacterController>().As<IStartable>().As<ITickable>();
    }
}