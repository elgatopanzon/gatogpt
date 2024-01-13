/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : TextGenerationService
 * @created     : Tuesday Jan 02, 2024 12:28:50 CST
 */

namespace GatoGPT.Service;

using GatoGPT.AI.TextGeneration;
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

public partial class TextGenerationService : Service
{
	private TextGenerationModelManager _modelManager;

	private Dictionary<string, AI.TextGeneration.IModelInstance> _modelInstances = new();

	private Queue<InferenceRequest> _inferenceQueue = new();

	public TextGenerationService()
	{
		// assign the model manager instance
		_modelManager = ServiceRegistry.Get<TextGenerationModelManager>();
	}

	/***********************
	*  Inference methods  *
	***********************/
	
	public AI.TextGeneration.IModelInstance Infer(string modelDefinitionId, string prompt, bool stateful = false, string existingInstanceId = "", AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null)
	{
		var modelInstance = QueueInferenceRequest(modelDefinitionId, prompt, stateful, existingInstanceId, loadParams, inferenceParams);	

		return modelInstance;
	}

	public InferenceResult InferWait(string modelDefinitionId, string prompt, bool stateful = false, string existingInstanceId = "", AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null)
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

	public async Task<InferenceResult> InferAsync(string modelDefinitionId, string prompt, bool stateful = false, string existingInstanceId = "", AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null)
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

	public AI.TextGeneration.IModelInstance QueueInferenceRequest(string modelDefinitionId, string prompt, bool stateful = false, string existingInstanceId = "", AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null)
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
	
	public AI.TextGeneration.IModelInstance CreateModelInstance(string modelDefinitionId, bool stateful = false, string existingInstanceId = "")
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

			modelInstance = (LlamaModelInstance) _modelInstances[existingInstanceId];
			modelInstance.InferenceResult = null;
		}

		// return the instance for external management
		return modelInstance;
	}


	public void AddModelInstance(AI.TextGeneration.IModelInstance instance)
	{
		_modelInstances.Add(instance.InstanceId, instance);
	}

	public void SetModelInstanceId(string instanceId, string newInstanceId)
	{
		_modelInstances[newInstanceId] = _modelInstances[instanceId];
		_modelInstances[newInstanceId].SetInstanceId(newInstanceId);
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

	public void DestroyExistingInstances(bool keepStateFiles = true)
	{
		LoggerManager.LogDebug("Destroying all instances", "", "keepStateFiles", keepStateFiles);

		foreach (var modelObj in _modelInstances)
		{
			if (modelObj.Value.SafeToUnloadModel())
			{
				modelObj.Value.DeleteInstanceState(keepStateFiles);
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

public partial class InferenceRequest
{
	public AI.TextGeneration.IModelInstance ModelInstance { get; set; }
	public string Prompt { get; set; }
	public AI.TextGeneration.LoadParams LoadParams { get; set; }
	public AI.TextGeneration.InferenceParams InferenceParams { get; set; }

	public InferenceRequest(AI.TextGeneration.IModelInstance modelInstance, string prompt, AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null) 
	{
		ModelInstance = modelInstance;
		Prompt = prompt;
		LoadParams = loadParams;
		InferenceParams = inferenceParams;
	}
}
