using Microsoft.AspNetCore.Builder;
using APIPlatform.Authentication.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using APIPlatform.Foundation.Extensions;
using APIPlatform.Logging.Extensions;
using APIPlatform.Configuration.Extensions;
using APIPlatform.Configuration.Options;
using APIPlatform.Validation.Extensions;
using APIPlatform.Validation.Abstractions;
using APIPlatform.Playground.Models;
using APIPlatform.Playground.Validators;
using APIPlatform.Playground.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAPIPlatformFoundation();
builder.Services.AddAPIPlatformLogging();
builder.Services.AddAPIPlatformConfiguration(builder.Configuration);
builder.Services.BindPlatformOptions<PlatformOptions>(builder.Configuration, "PlatformSettings");
builder.Services.AddAPIPlatformValidation();
builder.Services.AddAPIPlatformDatabase(builder.Configuration);
builder.Services.AddAPIPlatformAuthentication(builder.Configuration);
builder.Services.AddTransient<IValidator<SampleRequest>, SampleRequestValidator>();

builder.Services.AddScoped<APIPlatform.Playground.Services.PlaygroundValidationService>();
builder.Services.AddHostedService<APIPlatform.Playground.Services.PlaygroundInitializationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "APIPlatform Playground v1");
});

app.UseAuthentication();
app.UseCurrentUserContext();
app.UseAuthorization();

app.MapControllers();

app.Run();
