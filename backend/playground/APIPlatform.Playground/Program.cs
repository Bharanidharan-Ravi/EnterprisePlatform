using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using APIPlatform.Foundation.Extensions;
using APIPlatform.Logging.Extensions;
using APIPlatform.Configuration.Extensions;
using APIPlatform.Configuration.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAPIPlatformFoundation();
builder.Services.AddAPIPlatformLogging();
builder.Services.AddAPIPlatformConfiguration(builder.Configuration);
builder.Services.BindPlatformOptions<PlatformOptions>(builder.Configuration, "PlatformSettings");
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
