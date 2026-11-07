using FlowTrack.Api.Contracts.Authentication;
using FlowTrack.Api.Contracts.Tasks;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FlowTrack.Api.Swagger;

public sealed class SwaggerExamplesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(RegisterRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["userName"] = new OpenApiString("alex"),
                ["email"] = new OpenApiString("alex@example.com"),
                ["password"] = new OpenApiString("StrongPass1")
            };
        }
        else if (context.Type == typeof(LoginRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["email"] = new OpenApiString("alex@example.com"),
                ["password"] = new OpenApiString("StrongPass1")
            };
        }
        else if (context.Type == typeof(CreateTaskRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["title"] = new OpenApiString("Prepare release"),
                ["description"] = new OpenApiString("Publish release notes and deployment checklist."),
                ["dueDate"] = new OpenApiString("2030-01-15T17:00:00Z")
            };
        }
        else if (context.Type == typeof(UpdateTaskRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["title"] = new OpenApiString("Prepare release"),
                ["description"] = new OpenApiString("Publish release notes and deployment checklist."),
                ["status"] = new OpenApiString("InProgress"),
                ["dueDate"] = new OpenApiString("2030-01-15T17:00:00Z")
            };
        }
    }
}
