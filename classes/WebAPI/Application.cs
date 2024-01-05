/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : Application
 * @created     : Thursday Jan 04, 2024 20:16:27 CST
 */

namespace GatoGPT.WebAPI;

using GatoGPT.WebAPI.Extensions;
using GatoGPT.WebAPI.MappingProfiles;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.SwaggerGen;


public partial class Application
{
	private WebApplicationBuilder builder;
	private WebApplication app;

	public Application(string[] args, string host = "", int port = 0)
	{
		builder = WebApplication.CreateBuilder(args);

		// add services to the app
		builder.Services.AddControllers()
        	.AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver() { NamingStrategy = new SnakeCaseNamingStrategy() });

        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
		{
    		options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    		options.SerializerOptions.WriteIndented = true;
		});

        builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();
		builder.Services.AddSwaggerGenNewtonsoftSupport();

		builder.Services.AddCustomCors("AllowAllOrigins");

		// configure the Swagger UI
		builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

		builder.Services.AddRouting(options => options.LowercaseUrls = true);

		// configure API versions
		builder.Services.AddVersioning();

		// override the host and port
		if (port > 0 && host.Length > 0)
		{
			builder.WebHost.UseUrls($"http://{host}:{port}");
		}

		// add automatter services
		builder.Services.AddAutoMapper(typeof(ModelMappings));

		// build app
		app = builder.Build();

		// setup swagger UI for development version
		var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
		var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

		// Configure the HTTP request pipeline.
		if (app.Environment.IsDevelopment())
		{
    		app.UseSwagger();
    		app.UseSwaggerUI(
        		options =>
        		{
            		foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
            		{
                		options.SwaggerEndpoint(
                    		$"/swagger/{description.GroupName}/swagger.json",
                    		description.GroupName.ToUpperInvariant());
            		}
        		});
		} 
		else
		{
    		app.AddProductionExceptionHandling(loggerFactory);
		}

		app.UseCors("AllowAllOrigins");
		// app.UseHttpsRedirection();

		// app.UseAuthorization();

		// create routes for defined API controllers
		app.MapControllers();

		// launch the web app!
		app.Run();
	}
}

