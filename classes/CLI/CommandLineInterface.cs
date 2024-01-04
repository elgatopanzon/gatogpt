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

	public List<string> GetArgumentValues(string arg)
	{
		return _argsParsed.GetValueOrDefault(arg, new List<string>());
	}

	public string GetArgumentValue(string arg)
	{
		return GetArgumentValues(arg).SingleOrDefault("");
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

		// return a single inference request when not in chat mode
		if (!isChat)
		{
			// create a model instance
			LlamaModelInstance instance = _inferenceService.CreateModelInstance(modelId, stateful:false);
			string promptFull = instance.FormatPrompt(prompt);

			// subscribe to token events as they are generated and print them
			instance.SubscribeOwner<LlamaInferenceToken>((e) => {
				Console.Write(e.Token);
				});

			// print the full prompt when inference starts
			instance.SubscribeOwner<LlamaInferenceStart>(async (e) => {
				await Task.Delay(1000); // HACK: wait for the stateless context to log
				Console.WriteLine("");
				Console.WriteLine(promptFull);
				Console.WriteLine("");
				});

			// await for the inference result using the created instance ID
			InferenceResult result = await _inferenceService.InferAsync(modelId, prompt, stateful:false, existingInstanceId:instance.InstanceId);

			// print the final result
			Console.WriteLine("");
			Console.WriteLine("");
			Console.WriteLine($"GenerationTime: {result.GenerationTime.TotalMilliseconds} ms");
			Console.WriteLine($"PromptTokenCount: {result.PromptTokenCount}");
			Console.WriteLine($"GeneratedTokenCount: {result.GenerationTokenCount}");
			Console.WriteLine($"TotalTokenCount: {result.TotalTokenCount}");
			Console.WriteLine($"TimeToFirstToken: {result.TimeToFirstToken.TotalMilliseconds} ms");
			Console.WriteLine($"TokensPerSec: {result.TokensPerSec}");
		}

		return 0;
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

