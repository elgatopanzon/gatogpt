/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelDefinition
 * @created     : Monday Jan 01, 2024 23:58:19 CST
 */

namespace GatoGPT.AI;

using GatoGPT.Resource;
using GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Resource;

using GodotEGP.Objects.Validated;

// public partial class ModelDefinition : VObject
// {
// 	// friendly ID of the model definition
// 	private string _id;
// 	public string Id
// 	{
// 		get { return _id; }
// 		set { _id = value; }
// 	}
//
// 	// model profile preset code (used to override the filename one)
// 	internal readonly VValue<string> _profilePreset;
//
// 	public string ProfilePreset
// 	{
// 		get { return _profilePreset.Value; }
// 		set { _profilePreset.Value = value; }
// 	}
//
// 	// instance of the model resource to load for this model definition
// 	internal readonly VValue<Resource<LlamaModel>> _modelResource;
//
// 	public Resource<LlamaModel> ModelResource
// 	{
// 		get { return _modelResource.Value; }
// 		set { _modelResource.Value = value; }
// 	}
//
// 	internal readonly VValue<string> _modelResourceId;
//
// 	public string ModelResourceId
// 	{
// 		get { return _modelResourceId.Value; }
// 		set { _modelResourceId.Value = value; }
// 	}
//
// 	public ModelDefinition(string modelResourceId, string profilePreset = "", ModelProfile modelProfile = null)
// 	{
// 		_profilePreset = AddValidatedValue<string>(this)
// 		    .Default("")
// 		    .ChangeEventsEnabled();
//
// 		_modelResource = AddValidatedValue<Resource<LlamaModel>>(this)
// 		    .ChangeEventsEnabled();
//
// 		_modelResourceId = AddValidatedValue<string>(this)
// 		    .Default("")
// 		    .ChangeEventsEnabled();
//
// 		ModelResourceId = modelResourceId;
// 		ProfilePreset = profilePreset;
// 	}
// }


// WIP: generic class for model definition
public partial class ModelDefinition<TModelResource> : VObject where TModelResource : Resource
{
	// friendly ID of the model definition
	private string _id;
	public string Id
	{
		get { return _id; }
		set { _id = value; }
	}

	// model profile preset code (used to override the filename one)
	internal readonly VValue<string> _profilePreset;

	public string ProfilePreset
	{
		get { return _profilePreset.Value; }
		set { _profilePreset.Value = value; }
	}

	// instance of the model resource to load for this model definition
	internal readonly VValue<Resource<TModelResource>> _modelResource;

	public Resource<TModelResource> ModelResource
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


	internal readonly VValue<string> _ownedBy;

	public string OwnedBy
	{
		get { return _ownedBy.Value; }
		set { _ownedBy.Value = value; }
	}

	internal readonly VValue<string> _backend;

	public string Backend
	{
		get { return _backend.Value; }
		set { _backend.Value = value; }
	}

	public ModelDefinition(string modelResourceId, string profilePreset = "")
	{
		_profilePreset = AddValidatedValue<string>(this)
		    .Default("")
		    .ChangeEventsEnabled();

		_modelResource = AddValidatedValue<Resource<TModelResource>>(this)
		    .ChangeEventsEnabled();

		_modelResourceId = AddValidatedValue<string>(this)
		    .Default("")
		    .ChangeEventsEnabled();

		_ownedBy = AddValidatedValue<string>(this)
		    .Default("local")
		    .ChangeEventsEnabled();

		_backend = AddValidatedValue<string>(this)
		    .Default("builtin")
		    .ChangeEventsEnabled();

		ModelResourceId = modelResourceId;
		ProfilePreset = profilePreset;
	}
}
