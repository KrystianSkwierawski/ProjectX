using ProjectX.API;
using ProjectX.API.Infrastructure;
using ProjectX.Application;
using ProjectX.Infrastructure;
using ProjectX.Infrastructure.Persistance;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Configure(context.Configuration.GetSection("Kestrel"));
});

builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
    await app.InitialiseDatabaseAsync();
}

app.UseHsts();
app.UseHttpsRedirection();

if (Convert.ToBoolean(builder.Configuration.GetSection("API")["SwaggerEnabled"]))
{
    app.UseStaticFiles();

    app.UseSwaggerUi(settings =>
    {
        settings.Path = "/api";
        settings.DocumentPath = "/api/specification.json";
    });
}

app.Map("/", () => Results.Redirect("/api"));
app.MapEndpoints();

Log.Information("Starting ProjectX API");

app.Run();