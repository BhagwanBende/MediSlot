using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MediSlot.API.OpenApi;

// Swagger madhe "Authorize" button dakhavto (JWT token taknyasathi)
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            }
        };

        foreach (var operation in document.Paths.Values.SelectMany(p => p.Operations!))
        {
            operation.Value.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Value.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            });
        }

        return Task.CompletedTask;
    }
}