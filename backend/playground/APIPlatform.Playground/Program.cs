using Microsoft.AspNetCore.Builder;
using APIPlatform.Authentication.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using APIPlatform.Foundation.Extensions;
using APIPlatform.Foundation.Interfaces;
using APIPlatform.Logging.Extensions;
using APIPlatform.Configuration.Extensions;
using APIPlatform.Configuration.Options;
using APIPlatform.Validation.Extensions;
using APIPlatform.Validation.Abstractions;
using APIPlatform.Playground.Infrastructure;
using APIPlatform.Playground.Models;
using APIPlatform.Playground.Validators;
using APIPlatform.Playground.Extensions;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAPIPlatformFoundation();
builder.Services.AddAPIPlatformLogging();
builder.Services.AddAPIPlatformConfiguration(builder.Configuration);
builder.Services.BindPlatformOptions<PlatformOptions>(builder.Configuration, "PlatformSettings");
builder.Services.AddAPIPlatformValidation();
builder.Services.AddAPIPlatformDatabase(builder.Configuration);
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddAPIPlatformDatabaseMigration();
builder.Services.AddAPIPlatformAuthentication(builder.Configuration);
builder.Services.AddTransient<IValidator<SampleRequest>, SampleRequestValidator>();

builder.Services.AddScoped<APIPlatform.Playground.Services.PlaygroundValidationService>();
builder.Services.AddHostedService<APIPlatform.Playground.Services.PlaygroundInitializationService>();

// Phase 2: one generic entity (Employee) end-to-end through CrudEngine + Rbac.
builder.Services.AddEmployeeModule();

// Phase 2: no CORS was configured anywhere in the platform; the frontend test app
// (frontend/playground/ui-platform-playground, a Vite dev server) needs it to call this API
// from a different origin during local development.
const string FrontendDevCorsPolicy = "FrontendDevCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendDevCorsPolicy, policy => policy
        .WithOrigins("http://localhost:5190")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new APIPlatform.Playground.Infrastructure.ObjectToInferredTypesConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Lets Swagger UI's "Authorize" button attach a JWT to every request, so [Authorize] endpoints
    // (Protected, and any real CrudEngine endpoint) can be exercised from the docs page: paste just
    // the token from POST api/auth/login's response — Swashbuckle adds the "Bearer " prefix itself.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token from POST /api/auth/login here (no \"Bearer \" prefix needed)."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document, null), new List<string>() }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "APIPlatform Playground v1");
});

app.UseCors(FrontendDevCorsPolicy);
app.UseAuthentication();
app.UseCurrentUserContext();
app.UseAuthorization();

app.MapControllers();

app.Run();
