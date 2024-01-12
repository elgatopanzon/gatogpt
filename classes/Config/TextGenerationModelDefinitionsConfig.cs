/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LLMModelDefinitionsConfig
 * @created     : Tuesday Jan 02, 2024 00:05:41 CST
 */

namespace GatoGPT.Config;

using GatoGPT.AI;
using GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using GodotEGP.Objects.Validated;

using System.Collections.Generic;

public partial class TextGenerationModelDefinitionsConfig : VConfig
{
	// holds model definitions
	internal readonly VValue<Dictionary<string, LlamaModelDefinition>> _llamaModelDefinitions;

	public Dictionary<string, LlamaModelDefinition> LlamaModelDefinitions
	{
		get { return _llamaModelDefinitions.Value; }
		set { _llamaModelDefinitions.Value = value; }
	}


	public TextGenerationModelDefinitionsConfig()
	{
		_llamaModelDefinitions = AddValidatedValue<Dictionary<string, LlamaModelDefinition>>(this)
		    .Default(new Dictionary<string, LlamaModelDefinition>())
		    .ChangeEventsEnabled();
	}
}

