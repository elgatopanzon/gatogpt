/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaModelDefinition
 * @created     : Friday Jan 05, 2024 23:05:17 CST
 */

namespace GatoGPT.AI.TextGeneration;

using GatoGPT.Resource;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Objects.Validated;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

// WIP: new class using generic modeldefinition class
public partial class LlamaModelDefinition : ModelDefinition<LlamaModel>
{
	// the profile instance used for the model definition
	internal readonly VNative<ModelProfile> _modelProfile;

	public ModelProfile ModelProfile
	{
		get { return _modelProfile.Value; }
		set { _modelProfile.Value = value; }
	}

	internal readonly VNative<ModelProfile> _modelProfileOverride;

	public ModelProfile ModelProfileOverride
	{
		get { return _modelProfileOverride.Value; }
		set { _modelProfileOverride.Value = value; }
	}

	internal readonly VValue<bool> _promptCache;

	public bool PromptCache
	{
		get { return _promptCache.Value; }
		set { _promptCache.Value = value; }
	}

	internal readonly VValue<bool> _vision;

	public bool Vision
	{
		get { return _vision.Value; }
		set { _vision.Value = value; }
	}

	internal readonly VValue<List<DynamicCtxConfig>> _dynamicCtxConfigs;

	public List<DynamicCtxConfig> DynamicCtxConfigs
	{
		get { return _dynamicCtxConfigs.Value; }
		set { _dynamicCtxConfigs.Value = value; }
	}

	public LlamaModelDefinition(string modelResourceId, string profilePreset = "", ModelProfile modelProfile = null) : base(modelResourceId, profilePreset)
	{
		_modelProfile = AddValidatedNative<ModelProfile>(this)
		    .ChangeEventsEnabled();

		_modelProfileOverride = AddValidatedNative<ModelProfile>(this)
		    .ChangeEventsEnabled();

		_promptCache = AddValidatedValue<bool>(this)
		    .Default(false)
		    .ChangeEventsEnabled();

		_vision = AddValidatedValue<bool>(this)
		    .Default(true)
		    .ChangeEventsEnabled();

		_dynamicCtxConfigs = AddValidatedValue<List<DynamicCtxConfig>>(this)
		    .Default(new List<DynamicCtxConfig>())
		    .ChangeEventsEnabled();

		if (modelProfile != null)
		{
			ModelProfileOverride = modelProfile;
		}
	}
}

public class DynamicCtxConfig {
	public int NCtx { get; set; }
	public int NGpuLayers { get; set; }
	public int NThreads { get; set; }
}
