/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : GatoGPTCommandLineInterface
 * @created     : Sunday Jan 21, 2024 22:22:05 CST
 */

namespace GatoGPT.CLI;

using GatoGPT.Service;
using GatoGPT.AI;
using GatoGPT.AI.TextGeneration;
using GatoGPT.Config;
using GatoGPT.Event;
using GatoGPT.Resource;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.CLI;

using System.Text.RegularExpressions;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public partial class GatoGPTCLI : CommandLineInterface
{
	// services
	private TextGenerationService _inferenceService = ServiceRegistry.Get<TextGenerationService>();
	private TextGenerationModelManager _modelManager = ServiceRegistry.Get<TextGenerationModelManager>();

	public GatoGPTCLI(string[] args) : base(args)
	{
    	// add commands
    	_commands.Add("generate", (CommandGenerate, "Load a model and generate text", true));
    	_commands.Add("models", (CommandModels, "List configured models", true));
    	_commands.Add("api", (CommandApi, "Start the OpenAI API service", true));
    	_commands.Add("clean", (CommandClean, "Clean up cache and old states", true));

		// enable the testing command on debug build
    	if (OS.IsDebugBuild())
    	{
    		_commands.Add("testing", (CommandTest, "Run testing code", false));
    	}

    	// arg aliases
    	_argAliases.Add("-m", "--model");
    	_argAliases.Add("-p", "--prompt");
    	_argAliases.Add("-c", "--chat");

    	// command args
		_commandArgs.Add("models", new());
		_commandArgs.Add("generate", new());

		_commandArgs["generate"].Add(("--model", "MODEL_ID", "Model Definition ID to run generation with", true));
		_commandArgs["generate"].Add(("--prompt", "e.g. \"Hello there\"", "Prompt text to begin generation", true));
		_commandArgs["generate"].Add(("--chat", "", "Chat interactively with the model", false));
		_commandArgs["generate"].Add(("--n-ctx", "N", "Context size in tokens", false));
		_commandArgs["generate"].Add(("--image", "FILE", "Path to an image file (vision models)", false));
		_commandArgs["generate"].Add(("--n-batch", "N", "Batch size for token processing", false));
		_commandArgs["generate"].Add(("--n-gpu-layers", "N", "Number of layers to offload to GPU", false));
		_commandArgs["generate"].Add(("--main-gpu", "N", "GPU device ID to use for offloading", false));
		_commandArgs["generate"].Add(("--rope-freq-base", "N", "Rope Frequency Base", false));
		_commandArgs["generate"].Add(("--rope-freq-scale", "N", "Rope Frequency Scale", false));
		_commandArgs["generate"].Add(("--use-mlock", "", "Enable mlock call", false));
		_commandArgs["generate"].Add(("--no-mlock", "", "Use without mlock", false));
		_commandArgs["generate"].Add(("--use-mmap", "", "Enable mmap", false));
		_commandArgs["generate"].Add(("--no-mmap", "", "Use without mmap", false));
		_commandArgs["generate"].Add(("--seed", "N", "Seed used for generation", false));
		_commandArgs["generate"].Add(("--f16kv", "", "Use F16 k/v store", false));
		_commandArgs["generate"].Add(("--no-f16kv", "", "Disable F16 k/v store", false));
		_commandArgs["generate"].Add(("--kv-offload", "", "Offload KV store to GPU", false));
		_commandArgs["generate"].Add(("--no-kv-offload", "", "Disable offloading KV store to GPU", false));
		_commandArgs["generate"].Add(("--n-threads", "N", "CPU threads to use for inference", false));
		_commandArgs["generate"].Add(("--keep", "N", "Tokens to keep from initial prompt", false));
		_commandArgs["generate"].Add(("--n-predict", "N", "Max tokens to generate", false));
		_commandArgs["generate"].Add(("--top-k", "N", "Value for TopK", false));
		_commandArgs["generate"].Add(("--min-p", "N", "Value for MinP", false));
		_commandArgs["generate"].Add(("--top-p", "N", "Value for TopP", false));
		_commandArgs["generate"].Add(("--temperature", "N", "Controls amount of randomisation", false));
		_commandArgs["generate"].Add(("--frequency-penalty", "N", "Penalise for repeating frequent tokens", false));
		_commandArgs["generate"].Add(("--presence-penalty", "N", "Penalise for repeating existing tokens", false));
		_commandArgs["generate"].Add(("--repeat-penalty", "N", "Penalise for repeating tokens", false));
		_commandArgs["generate"].Add(("--antiprompts", "[string1] [string2...]", "List of strings to stop generation", false));
		_commandArgs["generate"].Add(("--input-prefix", "PREFIX", "Prefix to apply to prompt", false));
		_commandArgs["generate"].Add(("--input-suffix", "SUFFIX", "Suffix to apply to prompt", false));
		_commandArgs["generate"].Add(("--pre-prompt", "PRE_PROMPT", "System prompt to use before main prompt", false));
		_commandArgs["generate"].Add(("--pre-prompt-prefix", "PRE_PREFIX", "Prefix to apply to pre-prompt", false));
		_commandArgs["generate"].Add(("--pre-prompt-suffix", "PRE_SUFFIX", "Suffix to apply to pre-prompt", false));

		_commandArgs.Add("api", new());
		_commandArgs["api"].Add(("--host", "IP", "Host address to listen on", false));
		_commandArgs["api"].Add(("--port", "PORT", "Port to listen on", false));

		_commandArgs.Add("clean", new());

	}

	/**************
	*  Commands  *
	**************/

	public async Task<int> CommandGenerate()
	{
		string modelId = GetArgumentValue("--model");
		string prompt = GetArgumentValue("--prompt");

		bool isChat = GetArgumentSwitchValue("--chat");

		// check if model ID is valid
		if (modelId == "")
		{
			Console.WriteLine($"Error: --model must be set to a model id");
			return 1;
		}
		if (!_modelManager.ModelDefinitions.ContainsKey(modelId))
		{
			Console.WriteLine($"Error: no model exists with id '{modelId}'");
			return 1;
		}

		LoggerManager.LogDebug("Starting generation", "", "modelId", modelId);

		AI.TextGeneration.LoadParams loadParams = GetGenerationLoadParams();
		AI.TextGeneration.InferenceParams inferenceParams = GetGenerationInferenceParams();

		AI.TextGeneration.Backends.ITextGenerationBackend instance;
		bool isStateful = false;

		if (!isChat)
		{
			instance = _inferenceService.CreateModelInstance(modelId, stateful:false);
		}
		else
		{
			instance = _inferenceService.CreateModelInstance(modelId, stateful:true);
			isStateful = true;
		}

		// subscribe to token events as they are generated and print them
		instance.SubscribeOwner<TextGenerationInferenceToken>((e) => {
			Console.Write(e.Token);
			});

		// print the full prompt when inference starts
		instance.SubscribeOwner<TextGenerationInferenceStart>(async (e) => {
			await Task.Delay(1000); // HACK: wait for the stateless context to log

			if (instance.IsFirstRun)
			{
				// string prompt = instance.GetCurrentPrompt();
				Console.WriteLine("");
				// Console.WriteLine(prompt);
				Console.WriteLine("");
			}
			});

		// print empty line after inference finished
		instance.SubscribeOwner<TextGenerationInferenceFinished>((e) => {
			Console.Write("");
			});

		var fullPrompt = prompt;

		while(true)
		{
			// await for the inference result using the created instance ID
			InferenceResult result = await _inferenceService.InferAsync(modelId, fullPrompt, isStateful, existingInstanceId:instance.InstanceId, loadParams, inferenceParams);

			fullPrompt += " "+result.Output;

			// print the final result when not in chat mode
			if (!isChat)
			{
				Console.WriteLine("");
				Console.WriteLine($"GenerationTime: {result.GenerationTime.TotalMilliseconds} ms");
				Console.WriteLine($"PromptTokenCount: {result.PromptTokenCount}");
				Console.WriteLine($"GeneratedTokenCount: {result.GenerationTokenCount}");
				Console.WriteLine($"TotalTokenCount: {result.TotalTokenCount}");
				Console.WriteLine($"TimeToFirstToken: {result.TimeToFirstToken.TotalMilliseconds} ms");
				Console.WriteLine($"TokensPerSec: {result.TokensPerSec}");

				break;
			}
			else
			{
				Console.WriteLine("");
				Console.Write("> ");
				prompt = Console.ReadLine();
				if (prompt == null)
				{
					prompt = "";
				}

				if (prompt == "quit")
				{
					_inferenceService.DestroyExistingInstances();
					break;
				}

				fullPrompt += " "+prompt;
			}
		}

		return 0;
	}

	public LoadParams GetGenerationLoadParams()
	{
		var loadParams = new AI.TextGeneration.LoadParams();

		if (ArgExists("--n-ctx"))
			loadParams.NCtx = Convert.ToInt32(GetArgumentValue("--n-ctx", loadParams.NCtx.ToString()));
		if (ArgExists("--n-batch"))
			loadParams.NBatch = Convert.ToInt32(GetArgumentValue("--n-batch", loadParams.NBatch.ToString()));
		if (ArgExists("--rope-freq-base"))
			loadParams.RopeFreqBase = Convert.ToDouble(GetArgumentValue("--rope-freq-base", loadParams.RopeFreqBase.ToString()));
		if (ArgExists("--rope-freq-scale"))
			loadParams.RopeFreqScale = Convert.ToDouble(GetArgumentValue("--rope-freq-scale", loadParams.RopeFreqScale.ToString()));
		if (ArgExists("--n-gpu-layers"))
			loadParams.NGpuLayers = Convert.ToInt32(GetArgumentValue("--n-gpu-layers", loadParams.NGpuLayers.ToString()));
		if (ArgExists("--main-gpu"))
			loadParams.MainGpu = Convert.ToInt32(GetArgumentValue("--main-gpu", loadParams.MainGpu.ToString()));
		if (ArgExists("--seed"))
			loadParams.Seed = Convert.ToInt32(GetArgumentValue("--seed", loadParams.Seed.ToString()));

		if (GetArgumentSwitchValue("--use-mlock"))
		{
			loadParams.UseMlock = true;
		}
		if (GetArgumentSwitchValue("--no-mlock"))
		{
			loadParams.UseMlock = false;
		}
		if (GetArgumentSwitchValue("--use-mmap"))
		{
			loadParams.UseMMap = true;
		}
		if (GetArgumentSwitchValue("--no-mmap"))
		{
			loadParams.UseMMap = false;
		}
		if (GetArgumentSwitchValue("--f16kv"))
		{
			loadParams.F16KV = true;
		}
		if (GetArgumentSwitchValue("--no-f16kv"))
		{
			loadParams.F16KV = false;
		}
		if (GetArgumentSwitchValue("--kv-offload"))
		{
			loadParams.F16KV = true;
		}
		if (GetArgumentSwitchValue("--no-kv-offload"))
		{
			loadParams.F16KV = false;
		}

		return loadParams;
	}

	public InferenceParams GetGenerationInferenceParams()
	{
		var inferenceParams = new AI.TextGeneration.InferenceParams();

		if (ArgExists("--n-threads"))
			inferenceParams.NThreads = Convert.ToInt32(GetArgumentValue("--n-threads", inferenceParams.NThreads.ToString()));
		if (ArgExists("--keep"))
			inferenceParams.KeepTokens = Convert.ToInt32(GetArgumentValue("--keep", inferenceParams.KeepTokens.ToString()));
		if (ArgExists("--n-predict"))
			inferenceParams.NPredict = Convert.ToInt32(GetArgumentValue("--n-predict", inferenceParams.NPredict.ToString()));
		if (ArgExists("--top-k"))
			inferenceParams.TopK = Convert.ToInt32(GetArgumentValue("--top-k", inferenceParams.TopK.ToString()));
		if (ArgExists("--min-p"))
			inferenceParams.MinP = Convert.ToDouble(GetArgumentValue("--min-p", inferenceParams.MinP.ToString()));
		if (ArgExists("--top-p"))
			inferenceParams.TopP = Convert.ToDouble(GetArgumentValue("--top-p", inferenceParams.TopP.ToString()));
		if (ArgExists("--temperature"))
			inferenceParams.Temp = Convert.ToDouble(GetArgumentValue("--temperature", inferenceParams.Temp.ToString()));
		if (ArgExists("--frequency-penalty"))
			inferenceParams.FrequencyPenalty = Convert.ToDouble(GetArgumentValue("--frequency-penalty", inferenceParams.RepeatPenalty.ToString()));
		if (ArgExists("--presence-penalty"))
			inferenceParams.PresencePenalty = Convert.ToDouble(GetArgumentValue("--presence-penalty", inferenceParams.RepeatPenalty.ToString()));
		if (ArgExists("--repeat-penalty"))
			inferenceParams.RepeatPenalty = Convert.ToDouble(GetArgumentValue("--repeat-penalty", inferenceParams.RepeatPenalty.ToString()));
		if (ArgExists("--antiprompts"))
			inferenceParams.Antiprompts = inferenceParams.Antiprompts.Concat(GetArgumentValues("--antiprompts")).ToList();

		if (ArgExists("--input-prefix"))
			inferenceParams.InputPrefix = GetArgumentValue("--input-prefix", inferenceParams.InputPrefix);
		if (ArgExists("--input-suffix"))
			inferenceParams.InputSuffix = GetArgumentValue("--input-suffix", inferenceParams.InputSuffix);
		if (ArgExists("--pre-prompt"))
			inferenceParams.PrePrompt = GetArgumentValue("--pre-prompt", inferenceParams.PrePrompt);
		if (ArgExists("--pre-prompt-prefix"))
			inferenceParams.PrePromptPrefix = GetArgumentValue("--pre-prompt-prefix", inferenceParams.PrePromptPrefix);
		if (ArgExists("--pre-prompt-suffix"))
			inferenceParams.PrePromptSuffix = GetArgumentValue("--pre-prompt-suffix", inferenceParams.PrePromptSuffix);

		if (ArgExists("--image"))
			inferenceParams.ImagePath = GetArgumentValue("--image", "");

		return inferenceParams;
	}

	public async Task<int> CommandModels()
	{
		if (true) // TODO: if we add more commands check this is equal to "list"
		{
			// print model files
			Console.WriteLine("Model Resources:");

			List<string> printedIds = new();

			foreach (var m in _modelManager.ModelResources)
			{
				var r = m.Value;

				// skip the duplicate resource definitions
				if (printedIds.Contains(r.Id))
				{
					continue;
				}

				Console.WriteLine($"");
				Console.WriteLine($"# {r.Id} #");
				Console.WriteLine($"Path: {r.Definition.Path}");

				printedIds.Add(r.Id);
			}
			
			// print model definitions
			Console.WriteLine("Model Definitions:");

			foreach (var m in _modelManager.ModelDefinitions)
			{
				var d = m.Value;

				Console.WriteLine($"");
				Console.WriteLine($"# {m.Key} #");
				if (d.ModelResourceId != null)
				{
					Console.WriteLine($"Model Resource: {d.ModelResourceId}");
				}
				else
				{
					Console.WriteLine($"Model Resource: none");
				}
				if (d.ModelProfile != null)
				{
					Console.WriteLine($"Automatic Preset: {d.ModelProfile.Name}");
				}
				else
				{
					Console.WriteLine($"Automatic Preset: none");
				}
				Console.WriteLine($"Profile Preset Override: {d.ProfilePreset}");
				Console.WriteLine($"Backend: {d.Backend}");

				var jsonString = JsonConvert.SerializeObject(
        		d.ModelProfile, Formatting.Indented,
        		new JsonConverter[] {new StringEnumConverter()});

				Console.WriteLine($"Model Profile:\n{jsonString}");
			}
		}

		return 0;
	}

	public async Task<int> CommandApi()
	{
		LoggerManager.LogDebug("Starting web API");
		Console.WriteLine("Starting API server");

		string host = GetArgumentValue("--host", "");
		int port = Convert.ToInt32(GetArgumentValue("--port", "0"));

		if ((host.Length > 0 && port == 0) || (host.Length == 0 && port > 0))
		{
			Console.WriteLine("Error: Both --host and --port must be used together");
			return 1;
		}

		var webApi = new WebAPI.Application(_args, host, port);

		return 0;
	}

	public async Task<int> CommandClean()
	{
		LoggerManager.LogDebug("Cleaning up cache/old states");

		System.IO.DirectoryInfo directory = new DirectoryInfo(Path.Combine(OS.GetUserDataDir(), "State"));

		foreach(System.IO.FileInfo file in directory.GetFiles()) file.Delete();
    	foreach(System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);

		return 0;
	}

	public async Task<int> CommandTest()
	{
		LoggerManager.LogDebug("Running testing code");

		var testing = new CodeTesting(_args);
		return await testing.Run();
	}
}

