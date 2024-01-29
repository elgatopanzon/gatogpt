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

public partial class TextGenerationBackend : AI.ModelBackend, ITextGenerationBackend
{
	public AI.TextGeneration.ModelDefinition ModelDefinition { get; set; }
	public AI.TextGeneration.LoadParams LoadParams { get; set; }
	public AI.TextGeneration.InferenceParams InferenceParams { get; set; }
	public string Prompt { get; set; } = "";
	public string CurrentInferenceLine { get; set; } = "";
	public InferenceResult InferenceResult { get; set; }

	public TextGenerationBackend(AI.TextGeneration.ModelDefinition modelDefinition, bool isStateful = false) : base(modelDefinition, isStateful)
	{
		this.SubscribeOwner<TextGenerationInferenceStart>(_On_InferenceStart, true);
		this.SubscribeOwner<TextGenerationInferenceFinished>(_On_InferenceFinished, true);
		this.SubscribeOwner<TextGenerationInferenceToken>(_On_InferenceToken, true);
		this.SubscribeOwner<TextGenerationInferenceLine>(_On_InferenceLine, true);
	}

	public virtual void StartInference(string promptText, AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null) {
		LoggerManager.LogDebug("Dummy method!");
	}
	
	public static ITextGenerationBackend CreateBackend(AI.TextGeneration.ModelDefinition modelDefinition, bool isStateful = false)
	{
		string fqClassName = typeof(ITextGenerationBackend).FullName;
		fqClassName = fqClassName.Replace("."+nameof(ITextGenerationBackend), "");
		fqClassName = fqClassName+"."+modelDefinition.Backend+"Backend";

		LoggerManager.LogDebug("Creating model backend instance", "", "backend", fqClassName);

		Type t = Type.GetType(fqClassName);

		if (t == null)
		{
			throw new Exception($"Invalid model backend: '{modelDefinition.Backend}'");
		}
     	return (ITextGenerationBackend) Activator.CreateInstance(t, modelDefinition, isStateful);
	}

	public string FormatPrompt(string userPrompt)
	{
		var prePromptP = InferenceParams.PrePromptPrefix;
		var prePromptS = InferenceParams.PrePromptSuffix;
		var prePrompt = InferenceParams.PrePrompt;

		var InputP = InferenceParams.InputPrefix;
		var InputS = InferenceParams.InputSuffix;

		Dictionary<string, object> templateVars = new();
		templateVars.Add("PrePromptPrefix", InferenceParams.PrePromptPrefix);
		templateVars.Add("PrePromptSuffix", InferenceParams.PrePromptSuffix);
		templateVars.Add("PrePrompt", InferenceParams.PrePrompt);
		templateVars.Add("InputPrefix", InferenceParams.InputPrefix);
		templateVars.Add("InputSuffix", InferenceParams.InputSuffix);
		templateVars.Add("Input", userPrompt);

		string formattedPrompt = "";
		
		if (InferenceParams.TemplateType == "instruct")
		{
			formattedPrompt = InferenceParams.InstructTemplate;
		}
		else if (InferenceParams.TemplateType == "chat-instruct")
		{
			formattedPrompt = InferenceParams.ChatTemplate;
		}

		foreach (var var in templateVars)
		{
			formattedPrompt = formattedPrompt.Replace("{{ "+var.Key+" }}", (string) var.Value);
		}

		return formattedPrompt;
	}

	public string FormatOutput(string output)
	{
		// strip antiprompts from the output
		foreach (var antiprompt in ModelDefinition.ModelProfile.InferenceParams.Antiprompts)
		{
			output = output.Replace(antiprompt, "");
		}

		return output.Trim();
	}

	public virtual int[] TokenizeString(string content)
	{
		// this is a fake tokenize method using the 100,000 words = 75,000 words
		// estimate, it's the default when a native backend tokenize method
		// isn't implemented

		int fakeTokenCount = content.Split(new char[] { ' ', '!', '<', '>', '/', '?', '[', ']' }).Count();
		int[] fakeArray = new int[] {};
		Array.Resize<int>(ref fakeArray, fakeTokenCount);

		return fakeArray;
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

