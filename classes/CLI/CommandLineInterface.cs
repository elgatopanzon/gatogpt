/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CommandLineInterface
 * @created     : Thursday Jan 04, 2024 00:37:45 CST
 */

namespace GatoGPT.CLI;

using GatoGPT.Service;
using GatoGPT.LLM;
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

using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public partial class CommandLineInterface
{
	private string[] _args { get; set; }
	private Dictionary <string, List<string>> _argsParsed { get; set; }
	private Dictionary <string, string> _argAliases = new();

	private Dictionary<string, Func<Task<int>>> _commands = new();

	// services
	private LlamaInferenceService _inferenceService = ServiceRegistry.Get<LlamaInferenceService>();
	private LlamaModelManager _modelManager = ServiceRegistry.Get<LlamaModelManager>();

	public CommandLineInterface(string[] args)
	{
		_args = args;
		_argsParsed = ParseArgs();

    	LoggerManager.LogDebug("CLI arguments list", "", "args", _args);
    	LoggerManager.LogDebug("CLI arguments parsed", "", "argsParsed", _argsParsed);

    	// add commands
    	_commands.Add("help", CommandHelp);
    	_commands.Add("generate", CommandGenerate);
    	_commands.Add("models", CommandModels);
    	_commands.Add("api", CommandApi);

    	// arg aliases
    	_argAliases.Add("-m", "--model");
    	_argAliases.Add("-p", "--prompt");
    	_argAliases.Add("-c", "--chat");

		SetLogLevel();
	}

	public void SetLogLevel()
	{
		if (_argsParsed.ContainsKey("--log-level"))
		{
			string logLevelString = _argsParsed["--log-level"][0];

			Message.LogLevel logLevel = (Message.LogLevel) Enum.Parse(typeof(Message.LogLevel), logLevelString);

			LoggerManager.SetLogLevel(logLevel);

			LoggerManager.LogInfo("Log level set", "", "logLevel", logLevel.ToString());
		}
	}

	public async Task<int> Run()
	{
		if (_args.Count() >= 1)
		{
			// get the running command and remove from args
			string cmd = _args[0];
			_args = _args.Skip(1).ToArray();
			
			// invoke the matching command
			if (_commands.ContainsKey(cmd))
			{
				return await _commands[cmd]();
			}
		}

		return await CommandHelp();
	}

	public Dictionary<string, List<string>> ParseArgs()
	{
		Dictionary<string, List<string>> parsed = new();

		// loop over args matching args with - or --
		string currentCommand = "";
		List<string> currentValues = new();

		foreach (string argPart in _args)
		{
			// looking for - or --
			if (IsCommandSwitch(argPart))
			{
				LoggerManager.LogDebug("Found command arg", "", "cmd", argPart);

				currentCommand = argPart;
				currentValues = new();

				// set command from alias
				if (_argAliases.ContainsKey(currentCommand))
				{
					currentCommand = _argAliases[argPart];
				}
			}
			else
			{
				// if the command is empty, then consider this the main command
				if (currentCommand == "")
				{
					currentCommand = argPart;
				}
				else
				{
					LoggerManager.LogDebug("Adding command value", "", "value", argPart);

					currentValues.Add(argPart);
				}
			}

			// add and reset current command state when we encounter a new
			// command
			if (currentCommand != "")
			{
				if (parsed.ContainsKey(currentCommand))
				{
				}
				else
				{
					parsed.Add(currentCommand, currentValues);
				}
			}

		}

		LoggerManager.LogDebug("Parsed arguments", "", "argsParsed", parsed);

		return parsed;
	}

	public bool IsCommandSwitch(string cmd)
	{
		return Regex.IsMatch(cmd, "-[a-zA-Z0-9]+");
	}

	public bool ArgExists(string arg)
	{
		return (_argsParsed.ContainsKey(arg));
	}

	public List<string> GetArgumentValues(string arg)
	{
		return _argsParsed.GetValueOrDefault(arg, new List<string>());
	}

	public string GetArgumentValue(string arg, string defaultVal = "")
	{
		return GetArgumentValues(arg).SingleOrDefault(defaultVal);
	}

	public bool GetArgumentSwitchValue(string arg)
	{
		return _argsParsed.ContainsKey(arg);
	}

	/**************
	*  Commands  *
	**************/

	public async Task<int> CommandHelp()
	{
		Console.WriteLine("Help text (todo)");

		return 0;
	}

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

		LoadParams loadParams = GetGenerationLoadParams();
		InferenceParams inferenceParams = GetGenerationInferenceParams();

		LlamaModelInstance instance;
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
		instance.SubscribeOwner<LlamaInferenceToken>((e) => {
			Console.Write(e.Token);
			});

		// print the full prompt when inference starts
		instance.SubscribeOwner<LlamaInferenceStart>(async (e) => {
			await Task.Delay(1000); // HACK: wait for the stateless context to log

			if (instance.IsFirstRun())
			{
				string promptFull = instance.GetCurrentPrompt();
				Console.WriteLine("");
				Console.WriteLine(promptFull);
				Console.WriteLine("");
			}
			});

		// print empty line after inference finished
		instance.SubscribeOwner<LlamaInferenceFinished>((e) => {
			Console.Write("");
			});

		while(true)
		{
			// await for the inference result using the created instance ID
			InferenceResult result = await _inferenceService.InferAsync(modelId, prompt, isStateful, existingInstanceId:instance.InstanceId, loadParams, inferenceParams);

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
			}
		}

		return 0;
	}

	public LoadParams GetGenerationLoadParams()
	{
		var loadParams = new LoadParams();

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

		return loadParams;
	}

	public InferenceParams GetGenerationInferenceParams()
	{
		var inferenceParams = new InferenceParams();

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
				if (printedIds.Contains(m.Key))
				{
					continue;
				}

				Console.WriteLine($"");
				Console.WriteLine($"# {r.Id} #");
				Console.WriteLine($"Path: {r.Definition.Path}");
			}
			
			// print model definitions
			Console.WriteLine("Model Definitions:");

			foreach (var m in _modelManager.ModelDefinitions)
			{
				var d = m.Value;

				Console.WriteLine($"");
				Console.WriteLine($"# {d.Id} #");
				Console.WriteLine($"Model Resource: {d.ModelResourceId}");
				Console.WriteLine($"Automatic Preset: {d.ModelProfile.Name}");
				Console.WriteLine($"Profile Preset Override: {d.ProfilePreset}");

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
		Console.WriteLine("Api");

		return 0;
	}
}

