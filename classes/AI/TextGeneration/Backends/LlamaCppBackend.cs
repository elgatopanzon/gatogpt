/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaCpp
 * @created     : Saturday Jan 13, 2024 16:46:53 CST
 */

namespace GatoGPT.AI.TextGeneration.Backends;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class LlamaCppBackend : TextGenerationBackend
{
	public LlamaCppBackend(ModelDefinition modelDefinition, bool isStateful = false) : base(modelDefinition, isStateful)
	{
		ModelDefinition = modelDefinition;

		LoggerManager.LogDebug("Created llamacpp backend", "", "instanceId", InstanceId);
		LoggerManager.LogDebug("", "", "modelDefinition", ModelDefinition);
	}
}

