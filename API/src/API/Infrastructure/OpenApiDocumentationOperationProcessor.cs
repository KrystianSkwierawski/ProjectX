using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using NJsonSchema;
using NSwag;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace ProjectX.API.Infrastructure;

public sealed class OpenApiDocumentationOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        if (context is not AspNetCoreOperationProcessorContext aspNetCoreContext)
        {
            return true;
        }

        var endpointMetadata = aspNetCoreContext.ApiDescription.ActionDescriptor.EndpointMetadata;

        if (endpointMetadata is null)
        {
            return true;
        }

        var operation = context.OperationDescription.Operation;
        var summary = endpointMetadata.OfType<IEndpointSummaryMetadata>().LastOrDefault();
        var description = endpointMetadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault();

        operation.Summary = summary?.Summary ?? operation.Summary;
        operation.Description = description?.Description ?? operation.Description;

        ApplyRequestBodyDescription(operation, endpointMetadata);
        ApplyParameterDescriptions(operation, endpointMetadata);
        ApplyResponseDescriptions(operation, endpointMetadata);
        ApplyAuthorizationDocumentation(operation, endpointMetadata);

        return true;
    }

    private static void ApplyRequestBodyDescription(OpenApiOperation operation, IList<object> endpointMetadata)
    {
        var metadata = endpointMetadata.OfType<OpenApiRequestBodyDescriptionMetadata>().LastOrDefault();

        if (metadata is not null && operation.RequestBody is not null)
        {
            operation.RequestBody.Description = metadata.Description;
        }
    }

    private static void ApplyParameterDescriptions(OpenApiOperation operation, IList<object> endpointMetadata)
    {
        foreach (var metadata in endpointMetadata.OfType<OpenApiParameterDescriptionMetadata>())
        {
            var parameter = operation.Parameters.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, metadata.ParameterName, StringComparison.OrdinalIgnoreCase));

            if (parameter is not null)
            {
                parameter.Description = metadata.Description;
            }
        }
    }

    private static void ApplyResponseDescriptions(OpenApiOperation operation, IList<object> endpointMetadata)
    {
        var documentedResponses = endpointMetadata
            .OfType<OpenApiResponseDescriptionMetadata>()
            .ToDictionary(metadata => metadata.StatusCode.ToString(), metadata => metadata.Description);

        foreach (var response in operation.Responses)
        {
            response.Value.Description = documentedResponses.GetValueOrDefault(response.Key)
                ?? GetDefaultResponseDescription(response.Key);
        }
    }

    private static void ApplyAuthorizationDocumentation(OpenApiOperation operation, IList<object> endpointMetadata)
    {
        var authorizeData = endpointMetadata.OfType<IAuthorizeData>().ToArray();
        var allowsAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();

        if (authorizeData.Length == 0 || allowsAnonymous)
        {
            return;
        }

        operation.Responses.TryAdd(
            StatusCodes.Status401Unauthorized.ToString(),
            new OpenApiResponse { Description = "A valid JWT access token is required." });
        operation.Responses.TryAdd(
            StatusCodes.Status403Forbidden.ToString(),
            new OpenApiResponse { Description = "The authenticated principal does not satisfy the required authorization policy." });

        var policies = authorizeData
            .Select(metadata => metadata.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct()
            .ToArray();

        if (policies.Contains(ProjectX.Domain.Constants.Policies.ServerPlayerSession))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = PlayerSessionAuthorizationHandler.HeaderName,
                Kind = OpenApiParameterKind.Header,
                IsRequired = true,
                Description = "Opaque player-session credential issued to the dedicated server after one-time ticket redemption.",
                Schema = new JsonSchema { Type = JsonObjectType.String }
            });
        }

        if (policies.Length > 0)
        {
            operation.Description = $"{operation.Description}\n\nRequired authorization policy: {string.Join(" or ", policies)}.";
        }
    }

    private static string GetDefaultResponseDescription(string statusCode)
    {
        return statusCode switch
        {
            "200" => "The request completed successfully.",
            "201" => "The resource was created successfully.",
            "204" => "The request completed successfully and has no response body.",
            "400" => "The request is malformed or failed validation.",
            "401" => "Authentication failed or valid credentials are required.",
            "403" => "The authenticated principal is not allowed to perform this operation.",
            "404" => "The requested resource was not found.",
            "409" => "The request conflicts with the current resource state.",
            "429" => "The request rate limit was exceeded.",
            _ => "The request produced this response."
        };
    }
}
