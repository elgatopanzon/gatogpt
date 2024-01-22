namespace GatoGPT;

using GodotEGPNonGame.ServiceWorkers;

using GatoGPT.CLI;

using GatoGPT.Service;
using GatoGPT.Handler;
using GatoGPT.Config;
using GatoGPT.Resource;
using GatoGPT.AI.TextGeneration;
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

		// init LLMConfigHandler
		SceneTree.Instance.Root.AddChild(new LlamaConfigHandler());
		SceneTree.Instance.Root.AddChild(new EmbeddingConfigHandler());

		// wait for services to be ready
		if (!ServiceRegistry.WaitForServices(
					typeof(ConfigManager), 
					typeof(ResourceManager), 
					typeof(ScriptService),
					typeof(TextGenerationModelManager),
					typeof(EmbeddingModelManager)
					))
			{
			LoggerManager.LogCritical("Required services never became ready");

			return 0;
		}

		LoggerManager.LogInfo("Services ready");

		// create SceneTree service worker instance
		var serviceWorker = new SceneTreeServiceWorker();
		await serviceWorker.StartAsync(new CancellationToken());

		LoggerManager.LogInfo("GodotEGP ready!");

		// init LlamaCacheService
		ServiceRegistry.Get<LlamaCacheService>();

    	// CLI application
    	var cli = new GatoGPTCLI(args);

		// execute the CLI parser
		return await cli.Run();
    }
}
