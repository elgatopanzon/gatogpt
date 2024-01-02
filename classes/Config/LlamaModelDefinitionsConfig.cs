/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LLMModelDefinitionsConfig
 * @created     : Tuesday Jan 02, 2024 00:05:41 CST
 */

namespace GatoGPT.Config;

using GatoGPT.LLM;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using GodotEGP.Objects.Validated;

using System.Collections.Generic;

public partial class LlamaModelDefinitionsConfig : VConfig
{
	// holds model definitions
	internal readonly VValue<Dictionary<string, ModelDefinition>> _modelDefinitions;

	public Dictionary<string, ModelDefinition> ModelDefinitions
	{
		get { return _modelDefinitions.Value; }
		set { _modelDefinitions.Value = value; }
	}


	public LlamaModelDefinitionsConfig()
	{
		_modelDefinitions = AddValidatedValue<Dictionary<string, ModelDefinition>>(this)
		    .Default(new Dictionary<string, ModelDefinition>())
		    .ChangeEventsEnabled();
	}
}

