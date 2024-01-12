/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : EmbeddingModelDefinition
 * @created     : Friday Jan 05, 2024 23:22:38 CST
 */

namespace GatoGPT.AI.Embedding;

using GatoGPT.AI.TextGeneration;
using GatoGPT.Resource;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class EmbeddingModelDefinition : ModelDefinition<EmbeddingModel>
{
	public EmbeddingModelDefinition(string modelResourceId, string profilePreset = "") : base(modelResourceId, profilePreset)
	{
		
	}
}

