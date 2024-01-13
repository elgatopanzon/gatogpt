/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelInstance
 * @created     : Friday Jan 12, 2024 19:19:46 CST
 */

namespace GatoGPT.AI.TextGeneration.Backends;

using GatoGPT.Event;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ModelBackend : AI.ModelBackend, IModelBackend
{
	public AI.TextGeneration.ModelDefinition ModelDefinition { get; set; }
	public AI.TextGeneration.LoadParams LoadParams { get; set; }
	public AI.TextGeneration.InferenceParams InferenceParams { get; set; }
	public string Prompt { get; set; } = "";
	public string CurrentInferenceLine { get; set; } = "";
	public InferenceResult InferenceResult { get; set; }

	public ModelBackend(AI.TextGeneration.ModelDefinition modelDefinition, bool isStateful = false) : base(modelDefinition, isStateful)
	{
		this.SubscribeOwner<TextGenerationInferenceStart>(_On_InferenceStart, true);
		this.SubscribeOwner<TextGenerationInferenceFinished>(_On_InferenceFinished, true);
		this.SubscribeOwner<TextGenerationInferenceToken>(_On_InferenceToken, true);
		this.SubscribeOwner<TextGenerationInferenceLine>(_On_InferenceLine, true);
	}

	public virtual void StartInference(string promptText, AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null) {
		LoggerManager.LogDebug("Dummy method!");
	}
	

	/**********************
	*  Callback methods  *
	**********************/

	public virtual void _On_ModelLoadStart(LlamaModelLoadStart e)
	{
	}

	public virtual void _On_ModelLoadFinished(LlamaModelLoadFinished e)
	{
	}

	public virtual void _On_InferenceStart(TextGenerationInferenceStart e)
	{
	}
	public virtual void _On_InferenceFinished(TextGenerationInferenceFinished e)
	{
	}

	public virtual void _On_InferenceToken(TextGenerationInferenceToken e)
	{
	}

	public virtual void _On_InferenceLine(TextGenerationInferenceLine e)
	{
	}
}

