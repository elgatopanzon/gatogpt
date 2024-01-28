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

	public LlamaModelDefinition(string modelResourceId, string profilePreset = "", ModelProfile modelProfile = null) : base(modelResourceId, profilePreset)
	{
		_modelProfile = AddValidatedNative<ModelProfile>(this)
		    .ChangeEventsEnabled();

		_modelProfileOverride = AddValidatedNative<ModelProfile>(this)
		    .ChangeEventsEnabled();

		_promptCache = AddValidatedValue<bool>(this)
		    .Default(false)
		    .ChangeEventsEnabled();

		if (modelProfile != null)
		{
			ModelProfileOverride = modelProfile;
		}
	}
}

