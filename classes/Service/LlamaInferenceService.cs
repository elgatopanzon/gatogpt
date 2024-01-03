/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaInferenceService
 * @created     : Tuesday Jan 02, 2024 12:28:50 CST
 */

namespace GatoGPT.Service;

using GatoGPT.LLM;
using GatoGPT.Config;
using GatoGPT.Event;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using System.Collections.Generic;

public partial class LlamaInferenceService : Service
{
	private LlamaModelManager _modelManager;

	private Dictionary<string, LlamaModelInstance> _modelInstances = new();

	public LlamaInferenceService()
	{
		// assign the model manager instance
		_modelManager = ServiceRegistry.Get<LlamaModelManager>();
	}

	public LlamaModelInstance Infer(string modelDefinitionId, string prompt, bool stateful = false, string existingInstanceId = "")
	{
		// check if the requested definition is valid
		if (!_modelManager.ModelDefinitionIsValid(modelDefinitionId))
		{
			throw new InvalidModelDefinitionException($"'{modelDefinitionId}' is an invalid model definition!");
		}

		// obtain the definition and create an instance
		var modelDefinition = _modelManager.GetModelDefinition(modelDefinitionId);

		var modelInstance = new LlamaModelInstance(modelDefinition, stateful);

		// unload all existing models to free memory
		UnloadExistingModels(existingInstanceId);

		if (existingInstanceId.Length == 0)
		{
			// add model instance to dictionary using InstanceId
			AddModelInstance(modelInstance);
		}
		else {
			LoggerManager.LogDebug("Using existing stateful instance", "", "instanceId", existingInstanceId);

			modelInstance = _modelInstances[existingInstanceId];
		}

		// load the model and infer
		modelInstance.SubscribeOwner<LlamaModelLoadFinished>((e) => {
			// once model finished loading, run the inferance
			LoggerManager.LogDebug("Model instance loaded", "", "instanceId", e.Id);

			modelInstance.RunInference(prompt);
		}, oneshot:true);

		// load the model
		modelInstance.LoadModel();

		// return the instance for external management
		return modelInstance;
	}

	public void AddModelInstance(LlamaModelInstance instance)
	{
		_modelInstances.Add(instance.InstanceId, instance);
	}

	public void UnloadExistingModels(string excludeInstanceId = "")
	{
		foreach (var modelObj in _modelInstances)
		{
			if (modelObj.Value.SafeToUnloadModel() && modelObj.Value.InstanceId != excludeInstanceId)
			{
				LoggerManager.LogDebug("Unloading model instance", "", "instanceId", modelObj.Value.InstanceId);

				modelObj.Value.UnloadModel();
			}
		}
	}

	/****************
	*  Exceptions  *
	****************/
	
	public class InvalidModelDefinitionException : Exception
	{
		public InvalidModelDefinitionException() { }
		public InvalidModelDefinitionException(string message) : base(message) { }
		public InvalidModelDefinitionException(string message, Exception inner) : base(message, inner) { }
		protected InvalidModelDefinitionException(
			System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
				: base(info, context) { }
	}
}

