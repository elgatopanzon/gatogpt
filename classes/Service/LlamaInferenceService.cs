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

	private Queue<InferenceRequest> _inferenceQueue = new();

	public LlamaInferenceService()
	{
		// assign the model manager instance
		_modelManager = ServiceRegistry.Get<LlamaModelManager>();
	}

	/***********************
	*  Inference methods  *
	***********************/
	
	public LlamaModelInstance Infer(string modelDefinitionId, string prompt, bool stateful = false, string existingInstanceId = "", LLM.LoadParams loadParams = null, LLM.InferenceParams inferenceParams = null)
	{
		var modelInstance = QueueInferenceRequest(modelDefinitionId, prompt, stateful, existingInstanceId, loadParams, inferenceParams);	

		return modelInstance;
	}

	public InferenceResult InferWait(string modelDefinitionId, string prompt, bool stateful = false, string existingInstanceId = "", LLM.LoadParams loadParams = null, LLM.InferenceParams inferenceParams = null)
	{
		// skip the queue and create the instance
		var modelInstance = CreateModelInstance(modelDefinitionId, stateful, existingInstanceId);

		// start the inference
		modelInstance.StartInference(prompt);

		// wait indefinitely until finished
		while (!modelInstance.Finished)
		{
			System.Threading.Thread.Sleep(100);
		}

		return modelInstance.InferenceResult;
	}

	public async Task<InferenceResult> InferAsync(string modelDefinitionId, string prompt, bool stateful = false, string existingInstanceId = "", LLM.LoadParams loadParams = null, LLM.InferenceParams inferenceParams = null)
	{
		// call the normal infer which queues request
		var modelInstance = Infer(modelDefinitionId, prompt, stateful, existingInstanceId, loadParams, inferenceParams);	

		// wait for the model
		while (!modelInstance.Finished)
		{
			// LoggerManager.LogDebug("Waiting for inference", "", "inferenceRequest", $"prompt:{prompt}, model:{modelDefinitionId}, instanceId:{modelInstance.InstanceId}, queueSize:{_inferenceQueue.Count}");
			await Task.Delay(100);
		}

		return modelInstance.InferenceResult;
	}

	/*****************************
	*  Inference queue methods  *
	*****************************/

	public LlamaModelInstance QueueInferenceRequest(string modelDefinitionId, string prompt, bool stateful = false, string existingInstanceId = "", LLM.LoadParams loadParams = null, LLM.InferenceParams inferenceParams = null)
	{
		var modelInstance = CreateModelInstance(modelDefinitionId, stateful, existingInstanceId);

		LoggerManager.LogDebug("Queuing inference request", "", "inferenceRequest", $"prompt:{prompt}, model:{modelDefinitionId}");

		_inferenceQueue.Enqueue(new InferenceRequest(modelInstance, prompt, loadParams, inferenceParams));	

		return modelInstance;
	}

	public override void _Process(double delta)
	{
		// if there's a queued request and there's no running instances, then
		// dequeue a request and start inference
		if (_inferenceQueue.TryPeek(out var request) && !IsRunningInstances())
		{
			_inferenceQueue.Dequeue();

			LoggerManager.LogDebug("Running queued inference", "", "request", request);

			request.ModelInstance.StartInference(request.Prompt, request.LoadParams, request.InferenceParams);
		}
	}

	/****************************
	*  Model instance methods  *
	****************************/
	
	public LlamaModelInstance CreateModelInstance(string modelDefinitionId, bool stateful = false, string existingInstanceId = "")
	{
		// check if the requested definition is valid
		if (!_modelManager.ModelDefinitionIsValid(modelDefinitionId))
		{
			throw new InvalidModelDefinitionException($"'{modelDefinitionId}' is an invalid model definition!");
		}

		// obtain the definition and create an instance
		var modelDefinition = _modelManager.GetModelDefinition(modelDefinitionId);

		var modelInstance = new LlamaModelInstance(modelDefinition, stateful);

		if (existingInstanceId.Length == 0)
		{
			// add model instance to dictionary using InstanceId
			AddModelInstance(modelInstance);
		}
		else {
			LoggerManager.LogDebug("Using existing instance", "", "instanceId", existingInstanceId);

			modelInstance = _modelInstances[existingInstanceId];
			modelInstance.InferenceResult = null;
		}

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

	public bool IsRunningInstances()
	{
		foreach (var instance in _modelInstances)
		{
			if (instance.Value.Running)
			{
				return true;
			}
		}

		return false;
	}

	public void DestroyExistingInstances()
	{
		LoggerManager.LogDebug("Destroying all instances");

		foreach (var modelObj in _modelInstances)
		{
			modelObj.Value.DeleteInstanceState();
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

public partial class InferenceRequest
{
	public LlamaModelInstance ModelInstance { get; set; }
	public string Prompt { get; set; }
	public LLM.LoadParams LoadParams { get; set; }
	public LLM.InferenceParams InferenceParams { get; set; }

	public InferenceRequest(LlamaModelInstance modelInstance, string prompt, LLM.LoadParams loadParams = null, LLM.InferenceParams inferenceParams = null) 
	{
		ModelInstance = modelInstance;
		Prompt = prompt;
		LoadParams = loadParams;
		InferenceParams = inferenceParams;
	}
}
