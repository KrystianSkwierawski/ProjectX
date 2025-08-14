using ProjectX.API.Configuration;
using ProjectX.API.Container;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Configure(context.Configuration.GetSection("Kestrel"));
});

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

var container = new ServiceCollectionRegister()
    .AddConfiguration(builder.Services)
    .AddServices(builder.Services);

var app = builder.Build();

app.UseSwagger();

if (Convert.ToBoolean(builder.Configuration.GetSection("API")["SwaggerEnabled"]))
{
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

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    context.Response.Headers.Append("Cache-Control", "no-store");

    await next();
});

app.Run();