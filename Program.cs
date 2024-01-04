namespace GatoGPT;

using GodotEGPNonGame.ServiceWorkers;

using GatoGPT.Service;
using GatoGPT.Handler;
using GatoGPT.Config;
using GatoGPT.Resource;
using GatoGPT.LLM;
using GatoGPT.Event;

using GodotEGP;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Random;
using GodotEGP.Objects.Extensions;
using GodotEGP.Event.Events;
using GodotEGP.Event.Filter;
using Godot;

class Program
{
	public static GodotEGP.Main GodotEGP;

    static void Main(string[] args)
    {
		var builder = WebApplication.CreateBuilder(args);

		// Add services to the container.
		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();

		// background worker test
		builder.Services.AddHostedService<SceneTreeServiceWorker>();

		// force create the local user directory
		OS.GetUserDataDir();

		// init GodotEGP
		GodotEGP = new GodotEGP.Main();
		SceneTree.Instance.Root.AddChild(GodotEGP);

		var testnode = new TestNode();
		SceneTree.Instance.Root.AddChild(testnode);

		// var timertest = new Timer();
		// timertest.WaitTime = 5;
		// timertest.Autostart = true;
		// timertest.OneShot = false;
		// SceneTree.Instance.Root.AddChild(timertest);
		//

		// var rnd = new NumberGenerator(123, 1);
		// LoggerManager.LogDebug("Random int", "", "num", rnd.Randi());
		// LoggerManager.LogDebug("Random int", "", "num", rnd.Randi());
        //
		// var state = rnd.State;
        //
		// LoggerManager.LogDebug("Random int", "", "num", rnd.Randi());
		// LoggerManager.LogDebug("Random int", "", "num", rnd.Randi());
        //
		// rnd = new NumberGenerator(123, state);
        //
		// LoggerManager.LogDebug("Random int restored state", "", "num", rnd.Randi());
		// LoggerManager.LogDebug("Random int restored state", "", "num", rnd.Randi());

		var app = builder.Build();

		// Configure the HTTP request pipeline.
		if (app.Environment.IsDevelopment())
		{
    		app.UseSwagger();
    		app.UseSwaggerUI();
		}

		app.UseHttpsRedirection();

		var summaries = new[]
		{
    		"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		app.MapGet("/weatherforecast", () =>
		{
    		var forecast =  Enumerable.Range(1, 5).Select(index =>
        		new WeatherForecast
        		(
            		DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            		Random.Shared.Next(-20, 55),
            		summaries[Random.Shared.Next(summaries.Length)]
        		))
        		.ToArray();
    		return forecast;
		})
		.WithName("GetWeatherForecast")
		.WithOpenApi();

		// init LLMConfigHandler
		SceneTree.Instance.Root.AddChild(new LlamaConfigHandler());

		// wait for services to be ready
		if (ServiceRegistry.WaitForServices(
					typeof(ConfigManager), 
					typeof(ResourceManager), 
					typeof(ScriptService),
					typeof(LlamaModelManager)
					))
		{
			LoggerManager.LogDebug("Required services ready");

			// test LlamaInferenceService chaining instances
			// test stateless infer chain, copying the generated output to the
			// 2nd instance
			var instance = ServiceRegistry.Get<LlamaInferenceService>().Infer("testmodel", "Write 2 words about food", stateful:false);
			instance.Subscribe<LlamaInferenceFinished>((e) => {
				var instance = ServiceRegistry.Get<LlamaInferenceService>().Infer("testmodel", $"{e.Result.OutputStripped}\n\nWrite 2 more?", stateful:false);

				instance.Subscribe<LlamaInferenceFinished>((e) => {
					ServiceRegistry.Get<LlamaInferenceService>().DestroyExistingInstances();

					// test stateful infer chain, where the instance is the same and the
					// model will keep it's context
					var instance2 = ServiceRegistry.Get<LlamaInferenceService>().Infer("testmodel", "Write a 3 paragraph story about birds", stateful:true);

					instance2.Subscribe<LlamaInferenceFinished>((e) => {
						instance2 = ServiceRegistry.Get<LlamaInferenceService>().Infer("testmodel", "What is a good title for it?", stateful:true, instance2.InstanceId);

							instance2.Subscribe<LlamaInferenceFinished>((e) => {
								ServiceRegistry.Get<LlamaInferenceService>().DestroyExistingInstances();
							}, oneshot:true);
					}, oneshot:true);

				}, oneshot:true);
			}, oneshot:true);


			app.Run();
		}
		else
		{
			LoggerManager.LogCritical("Required services never became ready");
		}

    }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class TestNode : Node
{
	public override void _Ready()
	{
		LoggerManager.LogDebug("This node is ready!");
	}

	public override void _Process(double delta)
	{
		// LoggerManager.LogDebug("This node is being processed!", "", "delta", delta);
	}

	public void TestMethod()
	{
		LoggerManager.LogDebug("Test method");
	}

	public void TestCallDeferred()
	{
		CallDeferred("TestMethod");
	}
}
