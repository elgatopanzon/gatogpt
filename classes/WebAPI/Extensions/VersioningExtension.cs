/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : VersioningExtension
 * @created     : Thursday Jan 04, 2024 20:40:10 CST
 */

namespace GatoGPT.WebAPI.Extensions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;

public static class VersioningExtension
{
    public static void AddVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(
            config =>
            {
                config.ReportApiVersions = true;
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
                                                                                    new HeaderApiVersionReader("x-api-version"),
                                                                                    new MediaTypeApiVersionReader("x-api-version"));
            });
        services.AddVersionedApiExplorer(
            options =>
            {
                options.GroupNameFormat = "'v'VVV";

                // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                // can also be used to control the format of the API version in route templates
                options.SubstituteApiVersionInUrl = true;
            });
    }
}
