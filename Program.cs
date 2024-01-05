namespace GatoGPT;

using GodotEGPNonGame.ServiceWorkers;

using GatoGPT.CLI;

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

    static async Task<int> Main(string[] args)
    {
		// init GodotEGP
		GodotEGP = new GodotEGP.Main();
		SceneTree.Instance.Root.AddChild(GodotEGP);

		var serviceWorker = new SceneTreeServiceWorker();
		await serviceWorker.StartAsync(new CancellationToken());

		// init LLMConfigHandler
		SceneTree.Instance.Root.AddChild(new LlamaConfigHandler());

		// wait for services to be ready
		if (!ServiceRegistry.WaitForServices(
					typeof(ConfigManager), 
					typeof(ResourceManager), 
					typeof(ScriptService),
					typeof(LlamaModelManager)
					))
			{
			LoggerManager.LogCritical("Required services never became ready");

			return 0;
		}

		LoggerManager.LogDebug("GodotEGP ready!");

    	// CLI application
    	var cli = new CommandLineInterface(args);

		// execute the CLI parser
		return await cli.Run();
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
