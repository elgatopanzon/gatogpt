/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CustomCorsExtension
 * @created     : Thursday Jan 04, 2024 20:27:09 CST
 */

namespace GatoGPT.WebAPI.Extensions;

public static class CorsExtension
{
    public static void AddCustomCors(this IServiceCollection services, string policyName)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(policyName,
                builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });
    }
}
