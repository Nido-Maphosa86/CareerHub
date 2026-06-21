using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CareerHub.Api.Infrastructure.OpenApi;

// This transformer runs once, when the OpenAPI document is being built.
// It fills in the top-level information that .NET does not add by default:
// a proper title, a description of what the API does, contact info, and
// the server list shown at the top of the Scalar docs page.
//
// Registered in Program.cs with:
//   builder.Services.AddOpenApi(options =>
//       options.AddDocumentTransformer<CareerHubDocumentTransformer>());
public class CareerHubDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title       = "CareerHub API",
            Version     = "v1",
            Description =
                "A job board API. Employers post job listings, applicants search and apply for them. " +
                "Supports JWT authentication, full text search, rate limiting, pagination, and ETag caching " +
                "on individual job listings.",
            Contact = new OpenApiContact
            {
                Name  = "CareerHub Support",
                Email = "support@careerhub.example.com"
            }
        };

        // Listed here so anyone opening the docs page knows which base URL
        // the "Try it out" requests will actually be sent to.
        document.Servers =
        [
            new OpenApiServer
            {
                Url         = "http://localhost:5000",
                Description = "Local development"
            }
        ];

        return Task.CompletedTask;
    }
}
