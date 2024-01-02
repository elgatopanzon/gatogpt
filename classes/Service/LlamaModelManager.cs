/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LLMModelManager
 * @created     : Tuesday Jan 02, 2024 00:19:47 CST
 */

namespace GatoGPT.Service;

using GatoGPT.LLM;
using GatoGPT.Config;
using GatoGPT.Resource;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Resource;

using LLama;
using LLama.Common;

public partial class LlamaModelManager : Service
{
	private LlamaModelManagerConfig _config = new LlamaModelManagerConfig();
	private LlamaModelPresetsConfig _presetsConfig = new LlamaModelPresetsConfig();
	private LlamaModelDefinitionsConfig _definitionsConfig = new LlamaModelDefinitionsConfig();

	private Dictionary<string, Resource<LlamaModel>> _modelResources;

	public LlamaModelManager()
	{
		
	}

	public void SetConfig(LlamaModelManagerConfig config, LlamaModelPresetsConfig presetsConfig, LlamaModelDefinitionsConfig definitionsConfig)
	{
		LoggerManager.LogDebug("Setting config", "", "config", config);
		LoggerManager.LogDebug("Setting model presets config", "", "modelPresets", presetsConfig);
		LoggerManager.LogDebug("Setting model definitions config", "", "modelDefinitions", definitionsConfig);

		_config = config;
		_presetsConfig = presetsConfig;
		_definitionsConfig = definitionsConfig;

		if (!GetReady())
		{
			_SetServiceReady(true);
		}
	}

	public void SetModelResources(Dictionary<string, Resource<LlamaModel>> modelResources)
	{
		LoggerManager.LogDebug("Setting model resources config", "", "modelResources", modelResources);

		_modelResources = modelResources;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	// Called when service is registered in manager
	public override void _OnServiceRegistered()
	{
	}

	// Called when service is deregistered from manager
	public override void _OnServiceDeregistered()
	{
		// LoggerManager.LogDebug($"Service deregistered!", "", "service", this.GetType().Name);
	}

	// Called when service is considered ready
	public async override void _OnServiceReady()
	{
		LoggerManager.LogDebug("Model resources", "", "modelResources", _modelResources);
		LoggerManager.LogDebug("Model definitions", "", "modelDefinitions", _definitionsConfig);

		// test loading of a model using LLamaSharp
		// string modelPath = "/home/laz/text-generation-webui-docker/config/models/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/mistral-7b-instruct-v0.2.Q8_0.gguf"; // change it to your own model path
		// var prompt = "Transcript of a dialog, where the User interacts with an Assistant named Bob. Bob is helpful, kind, honest, good at writing, and never fails to answer the User's requests immediately and with precision.\r\n\r\nUser: Hello, Bob.\r\nBob: Hello. How may I help you today?\r\nUser: Please tell me the largest city in Europe.\r\nBob: Sure. The largest city in Europe is Moscow, the capital of Russia.\r\nUser: How are you?"; // use the "chat-with-bob" prompt here.
        //
		// // Load a model
		// var parameters = new ModelParams(modelPath)
		// {
    	// 	ContextSize = 8192,
    	// 	// Seed = 1337,
    	// 	GpuLayerCount = 0
		// };
		// using var model = LLamaWeights.LoadFromFile(parameters);
        //
		// // Initialize a chat session
		// using var context = model.CreateContext(parameters);
		// var ex = new InteractiveExecutor(context);
		// ChatSession session = new ChatSession(ex);
        //
        //
		//
		// while (prompt != "stop")
		// {
		// 	string res = "";
        //
		// 	await foreach (var text in session.ChatAsync(prompt, new LLama.Common.InferenceParams() { Temperature = 0.8f, AntiPrompts = new List<string> { "User:" } }))
        //
    	// 	{
    	// 		res += text;
    	// 	}
        //
    	// 	LoggerManager.LogDebug(res);
    	// 	break;
		// }
	}
}

