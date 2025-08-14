namespace ProjectX.API.Configuration;

public interface IAppConfiguration
{
    string ConnectionString { get; }

    string ApiKey { get; }

    bool SwaggerEnabled { get; }
}

public class AppConfiguration : IAppConfiguration
{
    public AppConfiguration(IConfiguration configuration)
    {
        ConnectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("Not defined connection string");
        ApiKey = configuration.GetValue<string?>("API:Key") ?? throw new ArgumentNullException("Not defined api key");
        SwaggerEnabled = configuration.GetValue<bool?>("API:SwaggerEnabled") ?? false;
    }

    public string ConnectionString { get; }

    public string ApiKey { get; }

    public bool SwaggerEnabled { get; }
}
