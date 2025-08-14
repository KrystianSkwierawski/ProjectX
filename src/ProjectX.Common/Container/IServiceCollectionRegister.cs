using Microsoft.Extensions.DependencyInjection;

namespace ProjectX.Common.Container;

public interface IServiceCollectionRegister
{
    IServiceCollectionRegister AddConfiguration();

    IServiceCollectionRegister AddServices();

    IServiceCollectionRegister AddApi();
}
