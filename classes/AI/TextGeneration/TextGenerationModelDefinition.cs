/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : TextGenerationModelDefinition
 * @created     : Friday Jan 12, 2024 16:43:19 CST
 */

namespace GatoGPT.AI.TextGeneration;

using GatoGPT.AI;
using GatoGPT.Resource;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Objects.Validated;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class TextGenerationModelDefinition : ModelDefinition<LlamaModel>
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

	public TextGenerationModelDefinition(string modelResourceId, string profilePreset = "", ModelProfile modelProfile = null) : base(modelResourceId, profilePreset)
	{
		_modelProfile = AddValidatedNative<ModelProfile>(this)
		    .ChangeEventsEnabled();

		_modelProfileOverride = AddValidatedNative<ModelProfile>(this)
		    .ChangeEventsEnabled();

		if (modelProfile != null)
		{
			ModelProfileOverride = modelProfile;
		}
	}
}
