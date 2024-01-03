/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LLMModelManager
 * @created     : Tuesday Jan 02, 2024 00:19:47 CST
 */

namespace GatoGPT.Service;

using GatoGPT.LLM;
using GatoGPT.Config;
using GatoGPT.Resource;
using GatoGPT.Event;

using Godot;
using GodotEGP;
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

		PrepareDefinitionConfigs();

		if (!GetReady())
		{
			_SetServiceReady(true);
		}
	}

	public void PrepareDefinitionConfigs()
	{
		// check there's resources and model definitions before processing
		if (_definitionsConfig.ModelDefinitions.Count == 0 || _modelResources == null || _modelResources.Count == 0)
		{
			return;
		}

		foreach (var def in _definitionsConfig.ModelDefinitions)
		{
			if (def.Value.ModelResourceId.Length > 0)
			{
				def.Value.Id = def.Key;
				
				// fetch the resource object from resources
				def.Value.ModelResource = GetModelResource(def.Value.ModelResourceId);

				// find matching preset for filename
				def.Value.ModelProfile = _presetsConfig.GetPresetForFilename(def.Value.ModelResource.Definition.Path);

				// merge profile with profile overrides, if set
				if (def.Value.ModelProfileOverride != null)
				{
					LoggerManager.LogDebug("Applying model profile overrides", "", "overrides", def.Value.ModelProfileOverride);

					def.Value.ModelProfile.MergeFrom(def.Value.ModelProfileOverride);
				}
			}
		}
	}

	public Resource<LlamaModel> GetModelResource(string resourceId)
	{
		return _modelResources[resourceId];
	}

	public void SetModelResources(Dictionary<string, Resource<LlamaModel>> modelResources)
	{
		LoggerManager.LogDebug("Setting model resources config", "", "modelResources", modelResources);

		_modelResources = modelResources;

		PrepareDefinitionConfigs();
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
	public override void _OnServiceReady()
	{
		LoggerManager.LogDebug("Model resources", "", "modelResources", _modelResources);
		LoggerManager.LogDebug("Model definitions", "", "modelDefinitions", _definitionsConfig);
	}

	/******************************
	*  Model management methods  *
	******************************/
	
	public bool ModelDefinitionIsValid(string id)
	{
		return _definitionsConfig.ModelDefinitions.ContainsKey(id);
	}

	public ModelDefinition GetModelDefinition(string id)
	{
		return _definitionsConfig.ModelDefinitions[id];
	}
}

