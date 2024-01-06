/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : EmbeddingsModelDefinitionsConfig
 * @created     : Friday Jan 05, 2024 23:20:27 CST
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

public partial class EmbeddingModelDefinitionsConfig : VConfig
{
	// holds model definitions
	internal readonly VValue<Dictionary<string, EmbeddingModelDefinition>> _modelDefinitions;

	public Dictionary<string, EmbeddingModelDefinition> ModelDefinitions
	{
		get { return _modelDefinitions.Value; }
		set { _modelDefinitions.Value = value; }
	}


	public EmbeddingModelDefinitionsConfig()
	{
		_modelDefinitions = AddValidatedValue<Dictionary<string, EmbeddingModelDefinition>>(this)
		    .Default(new Dictionary<string, EmbeddingModelDefinition>())
		    .ChangeEventsEnabled();
	}
}

