/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : TextGenerationService
 * @created     : Tuesday Jan 02, 2024 12:28:50 CST
 */

namespace GatoGPT.Service;

using GatoGPT.AI.TextGeneration;
using GatoGPT.AI.TextGeneration.Backends;
using GatoGPT.Config;
using GatoGPT.Event;
using GatoGPT.Resource;

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
	private ResourceManager _resourceManager;

	private Dictionary<string, AI.TextGeneration.Backends.ITextGenerationBackend> _modelInstances = new();

	private Queue<InferenceRequest> _inferenceQueue = new();

	public TextGenerationService()
	{
		// assign the model manager instance
		_modelManager = ServiceRegistry.Get<TextGenerationModelManager>();
		_resourceManager = ServiceRegistry.Get<ResourceManager>();
	}

	/***********************
	*  Inference methods  *
	***********************/
	
	public AI.TextGeneration.Backends.ITextGenerationBackend Infer(string modelDefinitionId, string prompt, bool stateful = false, string existingInstanceId = "", AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null)
	{
		var modelInstance = QueueInferenceRequest(modelDefinitionId, prompt, stateful, existingInstanceId, loadParams, inferenceParams);	

		return modelInstance;
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

	public AI.TextGeneration.Backends.ITextGenerationBackend QueueInferenceRequest(string modelDefinitionId, string prompt, bool stateful = false, string existingInstanceId = "", AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null)
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

			// set GrammarResource
			// TODO: put this in a better location?
			string grammarResourceId = request.ModelInstance.ModelDefinition.ModelProfileOverride.InferenceParams.GrammarResourceId;
			if (request.InferenceParams.GrammarResourceId.Length > 0)
			{
				grammarResourceId = request.InferenceParams.GrammarResourceId;
			}
			if (grammarResourceId.Length > 0)
			{
				var grammarResources = _resourceManager.GetResources<LlamaGrammar>();
				if (grammarResources.TryGetValue(grammarResourceId, out var grammarResource))
				{
					LoggerManager.LogDebug("Setting grammar resource", "", "grammarResource", grammarResource);
					request.InferenceParams.GrammarResource = grammarResource;
				}
			}


			request.ModelInstance.StartInference(request.Prompt, request.LoadParams, request.InferenceParams);
		}
	}

	/****************************
	*  Model instance methods  *
	****************************/
	
	public AI.TextGeneration.Backends.ITextGenerationBackend CreateModelInstance(string modelDefinitionId, bool stateful = false, string existingInstanceId = "")
	{
		// check if the requested definition is valid
		if (!_modelManager.ModelDefinitionIsValid(modelDefinitionId))
		{
			throw new InvalidModelDefinitionException($"'{modelDefinitionId}' is an invalid model definition!");
		}

		// obtain the definition and create an instance
		var modelDefinition = _modelManager.GetModelDefinition(modelDefinitionId);

		var modelInstance = AI.ModelBackend.CreateBackend<ITextGenerationBackend>(modelDefinition, stateful);

		if (existingInstanceId.Length == 0)
		{
			// add model instance to dictionary using InstanceId
			AddModelInstance(modelInstance);
		}
		else {
			LoggerManager.LogDebug("Using existing instance", "", "instanceId", existingInstanceId);

			modelInstance = (ITextGenerationBackend) _modelInstances[existingInstanceId];
			modelInstance.InferenceResult = null;
		}

		// return the instance for external management
		return modelInstance;
	}

	public void AddModelInstance(AI.TextGeneration.Backends.ITextGenerationBackend instance)
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

	public ITextGenerationBackend GetPersistentInstance(string modelDefinitionId, bool stateful = false)
	{
		foreach (var instance in _modelInstances)
		{
			if (instance.Value.Persistent && instance.Value.ModelDefinition.Id == modelDefinitionId)
			{
				LoggerManager.LogDebug("Found persistent instance", "", "modelId", modelDefinitionId);
				LoggerManager.LogDebug("", "", "instance", instance.Value);

				return instance.Value;
			}
		}

		return null;
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
	public AI.TextGeneration.Backends.ITextGenerationBackend ModelInstance { get; set; }
	public string Prompt { get; set; }
	public AI.TextGeneration.LoadParams LoadParams { get; set; }
	public AI.TextGeneration.InferenceParams InferenceParams { get; set; }

	public InferenceRequest(AI.TextGeneration.Backends.ITextGenerationBackend modelInstance, string prompt, AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null) 
	{
		ModelInstance = modelInstance;
		Prompt = prompt;
		LoadParams = loadParams;
		InferenceParams = inferenceParams;
	}
}
