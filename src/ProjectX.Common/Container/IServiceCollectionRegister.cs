using Microsoft.Extensions.DependencyInjection;

namespace ProjectX.Common.Container;

public interface IServiceCollectionRegister
{
    IServiceCollectionRegister AddConfiguration(IServiceCollection services);

    IServiceCollectionRegister AddServices(IServiceCollection services);
}
