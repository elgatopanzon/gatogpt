/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ExceptionExtension
 * @created     : Thursday Jan 04, 2024 20:46:05 CST
 */

namespace GatoGPT.WebAPI.Extensions;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using Microsoft.AspNetCore.Diagnostics;

public static class ExceptionExtension
{
    public static void AddProductionExceptionHandling(this IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";
                var errorFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (errorFeature != null)
                {
                    var logger = loggerFactory.CreateLogger("Global exception logger");
                    logger.LogError(500, errorFeature.Error, errorFeature.Error.Message);
                }

                await context.Response.WriteAsync("There was an error");
            });
        });
    }
}
