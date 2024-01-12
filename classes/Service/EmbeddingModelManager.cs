/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : EmbeddingModelManager
 * @created     : Friday Jan 05, 2024 22:47:23 CST
 */

namespace GatoGPT.Service;

using GatoGPT.AI.Embedding;
using GatoGPT.Config;
using GatoGPT.Resource;
using GatoGPT.Event;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Resource;

public partial class EmbeddingModelManager : Service
{
	private EmbeddingModelManagerConfig _config = new EmbeddingModelManagerConfig();
	// private EmbeddingModelPresetsConfig _presetsConfig = new EmbeddingModelPresetsConfig();
	private EmbeddingModelDefinitionsConfig _definitionsConfig = new EmbeddingModelDefinitionsConfig();

	public Dictionary<string, EmbeddingModelDefinition> ModelDefinitions { 
		get {
			return _definitionsConfig.ModelDefinitions;
		}
	}


	private Dictionary<string, Resource<EmbeddingModel>> _modelResources;

	public Dictionary<string, Resource<EmbeddingModel>> ModelResources { 
		get {
			return _modelResources;
		}
	}

	public void SetConfig(EmbeddingModelManagerConfig config, EmbeddingModelDefinitionsConfig definitionsConfig)
	{
		LoggerManager.LogDebug("Setting config", "", "config", config);
		// LoggerManager.LogDebug("Setting model presets config", "", "modelPresets", presetsConfig);
		LoggerManager.LogDebug("Setting model definitions config", "", "modelDefinitions", definitionsConfig);

		_config = config;
		// _presetsConfig = presetsConfig;
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
				// def.Value.ModelProfile = _presetsConfig.GetPresetForFilename(def.Value.ModelResource.Definition.Path);

				// merge profile with profile overrides, if set
				// if (def.Value.ModelProfileOverride != null)
				// {
				// 	LoggerManager.LogDebug("Applying model profile overrides", "", "overrides", def.Value.ModelProfileOverride);
                //
				// 	def.Value.ModelProfile.MergeFrom(def.Value.ModelProfileOverride);
				// }
			}
		}
	}

	public Resource<EmbeddingModel> GetModelResource(string resourceId)
	{
		return _modelResources[resourceId];
	}

	public void SetModelResources(Dictionary<string, Resource<EmbeddingModel>> modelResources)
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

	public EmbeddingModelDefinition GetModelDefinition(string id)
	{
		return _definitionsConfig.ModelDefinitions[id];
	}
}

