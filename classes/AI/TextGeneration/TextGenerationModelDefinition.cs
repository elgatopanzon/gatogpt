/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : TextGenerationModelDefinition
 * @created     : Friday Jan 12, 2024 17:44:45 CST
 */

namespace GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class TextGenerationModelDefinition : LlamaModelDefinition
{
	public TextGenerationModelDefinition(string modelResourceId, string profilePreset = "", LlamaModelProfile modelProfile = null) : base(modelResourceId, profilePreset, modelProfile)
	{
		
	}
}

