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
	internal readonly VNative<LlamaModelProfile> _modelProfile;

	public LlamaModelProfile ModelProfile
	{
		get { return _modelProfile.Value; }
		set { _modelProfile.Value = value; }
	}

	internal readonly VNative<LlamaModelProfile> _modelProfileOverride;

	public LlamaModelProfile ModelProfileOverride
	{
		get { return _modelProfileOverride.Value; }
		set { _modelProfileOverride.Value = value; }
	}

	public LlamaModelDefinition(string modelResourceId, string profilePreset = "", LlamaModelProfile modelProfile = null) : base(modelResourceId, profilePreset)
	{
		_modelProfile = AddValidatedNative<LlamaModelProfile>(this)
		    .ChangeEventsEnabled();

		_modelProfileOverride = AddValidatedNative<LlamaModelProfile>(this)
		    .ChangeEventsEnabled();

		if (modelProfile != null)
		{
			ModelProfileOverride = modelProfile;
		}
	}
}

