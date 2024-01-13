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

// TODO: stop depending on LlamaModelDefintion class
public partial class ModelDefinition : LlamaModelDefinition
{
	public ModelDefinition(string modelResourceId, string profilePreset = "", ModelProfile modelProfile = null) : base(modelResourceId, profilePreset, modelProfile)
	{
		Backend = "BuiltinLlama";
	}
}

