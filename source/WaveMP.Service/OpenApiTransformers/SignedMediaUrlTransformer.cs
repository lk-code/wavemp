using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using WaveMP.Service.Signatures;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.OpenApiTransformers;

/// <summary>
/// Describes the endpoints that accept signed media URLs (<see cref="SignedMediaAttribute"/>): the optional query
/// parameters of the signature, and the session token or the signature as alternative security requirements. Runs
/// after <see cref="BearerSecurityRequirementTransformer"/>.
/// </summary>
internal sealed class SignedMediaUrlTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Description.ActionDescriptor.EndpointMetadata.OfType<SignedMediaAttribute>().Any())
        {
            return Task.CompletedTask;
        }

        operation.Parameters ??= [];
        operation.Parameters.Add(Parameter(
            MediaSignatureDefaults.UserIdParameter,
            new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" },
            "Signed media URL: id of the user the URL was issued to."));
        operation.Parameters.Add(Parameter(
            MediaSignatureDefaults.SessionIdParameter,
            new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" },
            "Signed media URL: id of the session the URL was issued in."));
        operation.Parameters.Add(Parameter(
            MediaSignatureDefaults.ExpiresParameter,
            new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" },
            "Signed media URL: expiry in Unix seconds."));
        operation.Parameters.Add(Parameter(
            MediaSignatureDefaults.SignatureParameter,
            new OpenApiSchema { Type = JsonSchemaType.String },
            "Signed media URL: Base64Url-encoded signature."));

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(TokenDefaults.SecuritySchemeName, context.Document)] = [],
            },
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(MediaSignatureDefaults.AuthenticationScheme, context.Document)] =
                    [],
            },
        ];

        return Task.CompletedTask;
    }

    private static OpenApiParameter Parameter(string name, OpenApiSchema schema, string description)
    {
        return new OpenApiParameter
        {
            Name = name,
            In = ParameterLocation.Query,
            Required = false,
            Schema = schema,
            Description = description,
        };
    }
}
