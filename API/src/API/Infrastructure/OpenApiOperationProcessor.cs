using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using NJsonSchema;
using NSwag;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace ProjectX.API.Infrastructure;

public sealed class OpenApiOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        if (context is not AspNetCoreOperationProcessorContext aspNetCoreContext)
        {
            return true;
        }

        var metadata = aspNetCoreContext.ApiDescription.ActionDescriptor.EndpointMetadata;

        if (metadata is null)
        {
            return true;
        }

        var operation = context.OperationDescription.Operation;
        operation.Summary = metadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary ?? operation.Summary;
        operation.Description = metadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description ?? operation.Description;

        AddAuthorizationContract(operation, metadata);

        return true;
    }

    private static void AddAuthorizationContract(OpenApiOperation operation, IList<object> metadata)
    {
        var authorizeData = metadata.OfType<IAuthorizeData>().ToArray();

        if (authorizeData.Length == 0 || metadata.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        operation.Responses.TryAdd(
            StatusCodes.Status401Unauthorized.ToString(),
            new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.TryAdd(
            StatusCodes.Status403Forbidden.ToString(),
            new OpenApiResponse { Description = "Forbidden" });

        if (!authorizeData.Any(authorize => authorize.Policy == AuthorizationPolicies.ServerPlayerSession))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = PlayerSessionAuthorizationHandler.HeaderName,
            Kind = OpenApiParameterKind.Header,
            IsRequired = true,
            Description = "Server-issued player session credential.",
            Schema = new JsonSchema { Type = JsonObjectType.String }
        });
    }
}
