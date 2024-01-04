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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public partial class CommandLineInterface
{
	private string[] _args{ get; set; }

	private Dictionary<string, Func<Task<int>>> _commands = new();

	// services
	private LlamaInferenceService _inferenceService = ServiceRegistry.Get<LlamaInferenceService>();
	private LlamaModelManager _modelManager = ServiceRegistry.Get<LlamaModelManager>();

	public CommandLineInterface(string[] args)
	{
		_args = args;

    	LoggerManager.LogDebug("CLI arguments", "", "args", args);

    	// add commands
    	_commands.Add("help", CommandHelp);
    	_commands.Add("generate", CommandGenerate);
    	_commands.Add("models", CommandModels);
    	_commands.Add("api", CommandApi);
	}

	public async Task<int> Run()
	{
		string commandMatch = "";

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

	public async Task<int> CommandHelp()
	{
		Console.WriteLine("Help text (todo)");

		return 0;
	}

	public async Task<int> CommandGenerate()
	{
		Console.WriteLine("Generate");

		// TODO: parse commands and use them to call inference service

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

