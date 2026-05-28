using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Kongroo.Catalog.Api.OpenApi;

public sealed class BearerSecurityRequirementTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;
        var hasAllowAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();
        var hasAuthorize = endpointMetadata.OfType<IAuthorizeData>().Any();

        if (hasAllowAnonymous || !hasAuthorize)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, context.Document)] = [],
            }
        );

        return Task.CompletedTask;
    }
}

