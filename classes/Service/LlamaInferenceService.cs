/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaInferenceService
 * @created     : Tuesday Jan 02, 2024 12:28:50 CST
 */

namespace GatoGPT.Service;

using GatoGPT.LLM;
using GatoGPT.Config;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using System.Collections.Generic;

public partial class LlamaInferenceService
{
	private LlamaModelManager _modelManager;

	private Dictionary<string, LlamaModelInstance> _modelInstances = new();

	public LlamaInferenceService()
	{
		// assign the model manager instance
		_modelManager = ServiceRegistry.Get<LlamaModelManager>();
	}
}

