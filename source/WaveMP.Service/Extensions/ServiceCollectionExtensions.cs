using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;
using WaveMP.Service.Filters;
using WaveMP.Service.OpenApiTransformers;
using WaveMP.Service.Problems;
using WaveMP.Service.Tokens;
using WaveMP.Service.Validators;

namespace WaveMP.Service.Extensions;

public static class ServiceCollectionExtensions
{
    private const string DtoSuffix = "Dto";
    private const string PageSuffix = "Page";

    // Schemas of all Problem Details types of the document.
    private static readonly string[] _problemSchemaIds =
    [
        nameof(ProblemDetails),
        RemoveDtoSuffix(nameof(ValidationProblemDto))!,
    ];

    /// <summary>
    /// Configures JSON for minimal APIs and controllers alike: numbers are only read as JSON numbers (so the OpenAPI
    /// document describes them as plain integers) and enums are only read and written as strings, like the OpenAPI
    /// document describes them. Enum DTOs declare <see cref="Converters.StrictStringEnumConverter{TEnum}"/> themselves;
    /// the global converter covers every other enum.
    /// </summary>
    /// <remarks>
    /// Request bodies of the controllers ignore the C# keyword <c>required</c>: a missing member stays at its default
    /// and its data annotation (<c>[Required]</c>) reports it as <c>required</c> under its own field. The serializer would
    /// only reject the body as a whole. The OpenAPI document still describes these members as required.
    /// </remarks>
    public static IServiceCollection AddWaveMpJson(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options => ConfigureSerializer(options.SerializerOptions));
        services.Configure<JsonOptions>(options =>
        {
            ConfigureSerializer(options.JsonSerializerOptions);
            options.JsonSerializerOptions.TypeInfoResolver =
                (options.JsonSerializerOptions.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
                .WithAddedModifier(LeaveRequiredMembersToValidation);
        });
        return services;
    }

    /// <summary>
    /// Registers the controllers. Validation errors are written as <see cref="ValidationProblemDto"/>: every error has
    /// a machine-readable <see cref="ValidationErrorCodeDto"/> instead of a text, and its field is the JSON name of the
    /// member, e.g. <c>name</c> instead of the C# property <c>Name</c>, so clients can assign every error to its field.
    /// </summary>
    public static IServiceCollection AddWaveMpControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
            {
                options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
                // Before the provider of the framework, which then skips the data annotations.
                options.ModelValidatorProviders.Insert(0, new CodedModelValidatorProvider());
                ValidationProblems.UseCodes(options.ModelBindingMessageProvider);
                options.Filters.Add<ValidationProblemResultFilter>();
            })
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = ValidationProblems.CreateResponse);
        return services;
    }

    /// <summary>
    /// Registers Problem Details (RFC 9457). Every Problem Details response carries a machine-readable
    /// <see cref="ProblemCodeDto"/> in its member <c>code</c>, by default derived from the status code.
    /// </summary>
    public static IServiceCollection AddWaveMpProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            IDictionary<string, object?> extensions = context.ProblemDetails.Extensions;
            if (!extensions.ContainsKey(ProblemCodes.ExtensionName))
            {
                int statusCode = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
                extensions[ProblemCodes.ExtensionName] = ProblemCodes.ForStatusCode(statusCode);
            }
        });

        return services;
    }

    /// <summary>
    /// Registers the token-based authentication: every request with a valid session token (JWT, see
    /// <see cref="JwtTokenIssuer"/>) is authenticated while its session is active, and the policy
    /// <see cref="AuthorizationPolicies.Admin"/> for admin endpoints. The signing key is loaded on start by
    /// <see cref="SigningKeyStore.InitializeAsync"/>.
    /// </summary>
    public static IServiceCollection AddWaveMpAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<SigningKeyStore>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<SessionPrincipalValidator>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<SigningKeyStore>((options, keys) =>
            {
                // Keeps the claim names of the token (sub, sid) instead of mapping them to long URIs.
                options.MapInboundClaims = false;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = TokenDefaults.Issuer,
                    ValidAudience = TokenDefaults.Audience,
                    ValidAlgorithms = [TokenDefaults.Algorithm],
                    IssuerSigningKeyResolver = (_, _, _, _) => [keys.Key],
                    NameClaimType = WaveMpClaimTypes.Subject,
                    RoleClaimType = WaveMpClaimTypes.Role,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context => context.HttpContext.RequestServices
                        .GetRequiredService<SessionPrincipalValidator>()
                        .ValidateAsync(context),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Admin, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(nameof(UserRole.Admin)));

        return services;
    }

    /// <summary>
    /// Registers the OpenAPI document <c>v1</c>, served at <c>/openapi/v1.json</c>.
    /// </summary>
    public static IServiceCollection AddWaveMpOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            // Clients see "Health" instead of "HealthDto" and "ProfilePage" for a PageDto of profiles.
            options.CreateSchemaReferenceId = CreateSchemaReferenceId;

            // Wrapped XML comment lines become spaces, so the document is the same on every build platform.
            options.AddOperationTransformer<XmlCommentLineBreakTransformer>();
            options.AddSchemaTransformer<XmlCommentLineBreakTransformer>();
            options.AddSchemaTransformer<StringEnumSchemaTransformer>();
            options.AddDocumentTransformer(AddProblemCodeAsync);
            options.AddDocumentTransformer<BinaryContentSchemaTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<BearerSecurityRequirementTransformer>();

            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "WaveMP API";
                document.Info.Version = "v1";
                document.Info.Description = "REST API of WaveMP, a nostalgic media player.";
                return Task.CompletedTask;
            });
        });

        return services;
    }

    private static void ConfigureSerializer(JsonSerializerOptions options)
    {
        options.NumberHandling = JsonNumberHandling.Strict;
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    }

    private static void LeaveRequiredMembersToValidation(JsonTypeInfo typeInfo)
    {
        foreach (JsonPropertyInfo property in typeInfo.Properties)
        {
            property.IsRequired = false;
        }
    }

    private static string? CreateSchemaReferenceId(JsonTypeInfo typeInfo)
    {
        Type type = typeInfo.Type;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PageDto<>))
        {
            return RemoveDtoSuffix(type.GetGenericArguments()[0].Name) + PageSuffix;
        }

        return RemoveDtoSuffix(OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo));
    }

    private static string? RemoveDtoSuffix(string? id)
    {
        return id is not null && id.EndsWith(DtoSuffix, StringComparison.Ordinal) && id.Length > DtoSuffix.Length
            ? id[..^DtoSuffix.Length]
            : id;
    }

    /// <summary>
    /// Adds the required member <c>code</c> to the Problem Details schemas, referencing the named enum schema of
    /// <see cref="ProblemCodeDto"/>. Runs on the finished document, so the reference is kept as <c>$ref</c>.
    /// </summary>
    private static async Task AddProblemCodeAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        IDictionary<string, IOpenApiSchema>? schemas = document.Components?.Schemas;
        if (schemas is null)
        {
            return;
        }

        OpenApiSchema[] problemSchemas = [.. _problemSchemaIds
            .Select(id => schemas.TryGetValue(id, out IOpenApiSchema? schema) ? schema : null)
            .OfType<OpenApiSchema>()];
        if (problemSchemas.Length == 0)
        {
            return;
        }

        string codeSchemaId = RemoveDtoSuffix(nameof(ProblemCodeDto))!;
        if (!schemas.ContainsKey(codeSchemaId))
        {
            OpenApiSchema codeSchema = await context.GetOrCreateSchemaAsync(
                typeof(ProblemCodeDto),
                cancellationToken: cancellationToken);
            document.AddComponent(codeSchemaId, codeSchema);
        }

        foreach (OpenApiSchema schema in problemSchemas)
        {
            schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
            schema.Properties[ProblemCodes.ExtensionName] = new OpenApiSchemaReference(codeSchemaId, document);
            schema.Required ??= new HashSet<string>();
            schema.Required.Add(ProblemCodes.ExtensionName);
        }
    }
}
