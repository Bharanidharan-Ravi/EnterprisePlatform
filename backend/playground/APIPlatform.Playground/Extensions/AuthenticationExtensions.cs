using APIPlatform.Authentication.DependencyInjection;
using APIPlatform.Authentication.Interfaces;
using APIPlatform.Authentication.Jwt;
using APIPlatform.Playground.Resolvers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace APIPlatform.Playground.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Automatically generated summary.
    /// </summary>
    public static IServiceCollection AddAPIPlatformAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIdentityResolver, PlaygroundIdentityResolver>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));

        services.AddAuthenticationPlatform();
        
        var jwtOptions = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>();
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            if (jwtOptions != null)
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
                };
            }
        });

        return services;
    }
}
