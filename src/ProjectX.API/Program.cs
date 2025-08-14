using ProjectX.API.Configuration;
using ProjectX.API.Container;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Configure(context.Configuration.GetSection("Kestrel"));
});

var container = new ServiceCollectionRegister(builder.Services)
    .AddConfiguration()
    .AddServices()
    .AddApi();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

var app = builder.Build();

if (Convert.ToBoolean(builder.Configuration.GetSection("API")["SwaggerEnabled"]))
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectX.API");
        options.RoutePrefix = "swagger";
    });
}

app.UseMiddleware<ApiKeyMiddleware>();

app.UseHsts();
app.UseHttpsRedirection();

app.UseRouting();
app.MapControllers();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    context.Response.Headers.Append("Cache-Control", "no-store");

    await next();
});


Log.Information("Starting ProjectX API");

app.Run();