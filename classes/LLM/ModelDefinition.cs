/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelDefinition
 * @created     : Monday Jan 01, 2024 23:58:19 CST
 */

namespace GatoGPT.LLM;

using GatoGPT.Resource;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Resource;

using GodotEGP.Objects.Validated;

public partial class ModelDefinition : VObject
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

	// model profile preset code (used to override the filename one)
	internal readonly VValue<string> _profilePreset;

	public string ProfilePreset
	{
		get { return _profilePreset.Value; }
		set { _profilePreset.Value = value; }
	}

	// instance of the model resource to load for this model definition
	internal readonly VValue<Resource<LlamaModel>> _modelResource;

	public Resource<LlamaModel> ModelResource
	{
		get { return _modelResource.Value; }
		set { _modelResource.Value = value; }
	}

	internal readonly VValue<string> _modelResourceId;

	public string ModelResourceId
	{
		get { return _modelResourceId.Value; }
		set { _modelResourceId.Value = value; }
	}

	public ModelDefinition(string modelResourceId, string profilePreset = "", ModelProfile modelProfile = null)
	{
		_modelProfile = AddValidatedNative<ModelProfile>(this)
		    .ChangeEventsEnabled();

		_modelProfileOverride = AddValidatedNative<ModelProfile>(this)
		    .ChangeEventsEnabled();

		_profilePreset = AddValidatedValue<string>(this)
		    .Default("")
		    .ChangeEventsEnabled();

		_modelResource = AddValidatedValue<Resource<LlamaModel>>(this)
		    .ChangeEventsEnabled();

		_modelResourceId = AddValidatedValue<string>(this)
		    .Default("")
		    .ChangeEventsEnabled();

		ModelResourceId = modelResourceId;
		ProfilePreset = profilePreset;

		if (modelProfile != null)
		{
			ModelProfileOverride = modelProfile;
		}
	}
}

