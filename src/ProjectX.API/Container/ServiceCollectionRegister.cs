using ProjectX.API.Configuration;
using ProjectX.Common.Container;
using ProjectX.Model;
using ProjectX.Services.Areas.Player.Services;

namespace ProjectX.API.Container;

public class ServiceCollectionRegister : IServiceCollectionRegister
{
    private readonly IServiceCollection _services;

    public ServiceCollectionRegister(IServiceCollection services)
    {
        _services = services;
    }

    public IServiceCollectionRegister AddConfiguration()
    {
        _services.AddSingleton<IAppConfiguration, AppConfiguration>();

        return this;
    }

    public IServiceCollectionRegister AddServices()
    {
        _services.AddScoped<IPlayerService, PlayerService>();
        _services.AddScoped<IPlayerPositionService, PlayerPositionService>();

        _services.AddScoped<Func<ProjectXContext>>(sp =>
        {
            var config = sp.GetRequiredService<IAppConfiguration>();
            return () => new ProjectXContext(config.ConnectionString);
        });

        return this;
    }

    public IServiceCollectionRegister AddApi()
    {
        _services.AddHttpContextAccessor();
        _services.AddControllers();
        _services.AddEndpointsApiExplorer();
        _services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "ProjectX API",
                Version = "v1",
                Description = "API for ProjectX"
            });

            //options.CustomSchemaIds(type => type.FullName);
        });

        _services.AddSingleton<ApiKeyMiddleware>();

        return this;
    }
}
