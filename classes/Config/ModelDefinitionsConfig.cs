/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelDefinitionsConfig
 * @created     : Friday Jan 12, 2024 17:27:36 CST
 */

namespace GatoGPT.Config;

using GatoGPT.AI.TextGeneration;
using GatoGPT.AI.Embedding;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Objects.Validated;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ModelDefinitionsConfig : VConfig
{
	// holds llama model definitions
	internal readonly VValue<Dictionary<string, ModelDefinition>> _textGenerationModelDefinitions;

	public Dictionary<string, ModelDefinition> TextGeneration
	{
		get { return _textGenerationModelDefinitions.Value; }
		set { _textGenerationModelDefinitions.Value = value; }
	}

	// holds embedding model definitions
	internal readonly VValue<Dictionary<string, EmbeddingModelDefinition>> _embeddingModelDefinitions;

	public Dictionary<string, EmbeddingModelDefinition> Embedding
	{
		get { return _embeddingModelDefinitions.Value; }
		set { _embeddingModelDefinitions.Value = value; }
	}

	public ModelDefinitionsConfig()
	{
		_textGenerationModelDefinitions = AddValidatedValue<Dictionary<string, ModelDefinition>>(this)
		    .Default(new Dictionary<string, ModelDefinition>())
		    .ChangeEventsEnabled();

		_textGenerationModelDefinitions.MergeCollections = true;

		_embeddingModelDefinitions = AddValidatedValue<Dictionary<string, EmbeddingModelDefinition>>(this)
		    .Default(new Dictionary<string, EmbeddingModelDefinition>())
		    .ChangeEventsEnabled();

		_embeddingModelDefinitions.MergeCollections = true;
	}
}

