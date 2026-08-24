using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProjectX.Application.Common.Behaviours;

namespace ProjectX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            configuration.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            configuration.AddOpenBehavior(typeof(LoggingBehaviour<,>));
        });

        return services;
    }
}
