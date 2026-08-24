using ProjectX.API;
using ProjectX.API.Infrastructure;
using ProjectX.Application;
using ProjectX.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Configure(context.Configuration.GetSection("Kestrel"));
});

builder.Services.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment()
    && !app.Configuration.GetValue<bool>("SkipDatabaseInitialization"))
{
    await app.InitialiseDatabaseAsync();
}

app.UseHsts();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();

    app.UseSwaggerUi(settings =>
    {
        settings.Path = "/api";
        settings.DocumentPath = "/api/specification.json";
    });

    app.Map("/", () => Results.Redirect("/api"));
}

app.MapEndpoints();

Log.Information("Starting ProjectX API");

app.Run();

public partial class Program
{
}
