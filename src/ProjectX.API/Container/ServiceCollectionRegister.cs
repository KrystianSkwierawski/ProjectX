using ProjectX.API.Configuration;
using ProjectX.Common.Container;
using ProjectX.Model;
using ProjectX.Services.Areas.Services;

namespace ProjectX.API.Container;

public class ServiceCollectionRegister : IServiceCollectionRegister
{
    public IServiceCollectionRegister AddConfiguration(IServiceCollection services)
    {
        services.AddSingleton<IAppConfiguration, AppConfiguration>();

        return this;
    }

    public IServiceCollectionRegister AddServices(IServiceCollection services)
    {
        services.AddSingleton<ApiKeyMiddleware>();

        services.AddScoped<IPlayerService>();

        services.AddScoped<Func<ProjectXContext>>(sp =>
        {
            var config = sp.GetRequiredService<IAppConfiguration>();
            return () => new ProjectXContext(config.ConnectionString);
        });

        return this;
    }
}
