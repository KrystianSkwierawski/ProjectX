namespace ProjectX.API.Infrastructure;

public static class OpenApiDocumentationExtensions
{
    public static RouteHandlerBuilder WithRequestBodyDescription(
        this RouteHandlerBuilder builder,
        string description)
    {
        return builder.WithMetadata(new OpenApiRequestBodyDescriptionMetadata(description));
    }

    public static RouteHandlerBuilder WithParameterDescription(
        this RouteHandlerBuilder builder,
        string parameterName,
        string description)
    {
        return builder.WithMetadata(new OpenApiParameterDescriptionMetadata(parameterName, description));
    }

    public static RouteHandlerBuilder WithResponseDescription(
        this RouteHandlerBuilder builder,
        int statusCode,
        string description)
    {
        return builder.WithMetadata(new OpenApiResponseDescriptionMetadata(statusCode, description));
    }
}

internal sealed record OpenApiRequestBodyDescriptionMetadata(string Description);

internal sealed record OpenApiParameterDescriptionMetadata(string ParameterName, string Description);

internal sealed record OpenApiResponseDescriptionMetadata(int StatusCode, string Description);
