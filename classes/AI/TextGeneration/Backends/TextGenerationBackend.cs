/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelInstance
 * @created     : Friday Jan 12, 2024 19:19:46 CST
 */

namespace GatoGPT.AI.TextGeneration.Backends;

using GatoGPT.Event;
using GatoGPT.AI.TextGeneration.TokenFilter;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using System.Text.RegularExpressions;

public partial class TextGenerationBackend : AI.ModelBackend, ITextGenerationBackend
{
	public AI.TextGeneration.ModelDefinition ModelDefinition { get; set; }
	public AI.TextGeneration.LoadParams LoadParams { get; set; }
	public AI.TextGeneration.InferenceParams InferenceParams { get; set; }
	public string Prompt { get; set; } = "";
	public string CurrentInferenceLine { get; set; } = "";
	public InferenceResult InferenceResult { get; set; }

	public StreamingTokenFilter StreamingTokenFilter  { get; set; }

	public TextGenerationBackend(AI.TextGeneration.ModelDefinition modelDefinition, bool isStateful = false) : base(modelDefinition, isStateful)
	{
		this.SubscribeOwner<TextGenerationInferenceStart>(_On_InferenceStart, true);
		this.SubscribeOwner<TextGenerationInferenceFinished>(_On_InferenceFinished, true);
		this.SubscribeOwner<TextGenerationInferenceToken>(_On_InferenceToken, true);
		this.SubscribeOwner<TextGenerationInferenceLine>(_On_InferenceLine, true);
	}

	public virtual void StartInference(string promptText, AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null) {
		Prompt = promptText;

		// set inference params and clone from provided ones
		InferenceParams = ModelDefinition.ModelProfile.InferenceParams.DeepCopy();
		LoadParams = ModelDefinition.ModelProfile.LoadParams.DeepCopy();

		// if we parsed any inference params, merge them into the copy of the
		// model profile's ones
		if (loadParams != null)
		{
			LoadParams.MergeFrom(loadParams);
		}
		if (inferenceParams != null)
		{
			InferenceParams.MergeFrom(inferenceParams);
		}
		
		// set to running state
		Running = true;

		LoggerManager.LogDebug("Starting inference", "", "prompt", Prompt);

		InitStreamingTokenFilter();

		_state.Transition(LOAD_MODEL_STATE);
	}

	public async virtual Task<bool> ExecuteInference()
	{
		return true;
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

		var inputP = InferenceParams.InputPrefix;
		var inputS = InferenceParams.InputSuffix;

		// remove preprompt prefix/suffix when preprompt is empty
		if (prePrompt.Length == 0)
		{
			prePromptP = "";
			prePromptS = "";
		}

		Dictionary<string, object> templateVars = new();
		templateVars.Add("PrePromptPrefix", prePromptP);
		templateVars.Add("PrePromptSuffix", prePromptS);
		templateVars.Add("PrePrompt", prePrompt);
		templateVars.Add("InputPrefix", inputP);
		templateVars.Add("InputSuffix", inputS);
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

		output = Regex.Replace(output, @"\p{C}+", string.Empty);

		return output.Trim();
	}

	public virtual List<TokenizedString> TokenizeString(string content, bool skipBos = true)
	{
		// this is a fake tokenize method using the 100,000 words = 75,000 words
		// estimate, it's the default when a native backend tokenize method
		// isn't implemented

		int fakeTokenCount = content.Split(new char[] { ' ', '!', '<', '>', '/', '?', '[', ']' }).Count();
		fakeTokenCount = Convert.ToInt32(((double) fakeTokenCount) * 2);
		int[] fakeArray = new int[] {};
		Array.Resize<int>(ref fakeArray, fakeTokenCount);

		List<TokenizedString> fakeDict = new();

		for (int i = 0; i < fakeArray.Count(); i++)
		{
			fakeDict.Add(new() {
				Id = i,
				Token = fakeArray[i].ToString() 
			});
		}

		return fakeDict;
	}

	public void VerifyPromptCacheLength()
	{
		// check for prompt exceeding token size
		int promptTokenLength = TokenizeString(FormatPrompt(Prompt)+InferenceParams.NegativeCfgPrompt).Count();

		LoggerManager.LogDebug("Prompt token size", "", "tokenSize", promptTokenLength);

		if (promptTokenLength > (LoadParams.NCtx - InferenceParams.NPredict))
		{
			throw new PromptExceedsContextLengthException($"Prompt length of {promptTokenLength} exceeds {LoadParams.NCtx - InferenceParams.NPredict} (NCtx {LoadParams.NCtx} - NPredict {InferenceParams.NPredict})");
		}
	}

	/**************************
	*  Token filter methods  *
	**************************/
	public void InitStreamingTokenFilter()
	{
		StreamingTokenFilter = new();		

		AddTokenFilter(new StripLeadingSpace());
		AddTokenFilter(new StripAntiprompt(InferenceParams.Antiprompts));
		// AddTokenFilter(new CaptureMarkdownOutput());
	}

	public void AddTokenFilter(ITokenFilter filter)
	{
		StreamingTokenFilter.AddFilter(filter);
	}

	public bool FilterToken(string token)
	{
		bool tokenFiltered = StreamingTokenFilter.FilterToken(token, InferenceResult.Tokens.ToArray());

		// check for released tokens
		if (!tokenFiltered)
		{
			var releasedTokens = StreamingTokenFilter.ReleasedTokens;

			if (releasedTokens.Count > 0)
			{
				LoggerManager.LogDebug("Filtered tokens after processing", "", "defilteredTokens", releasedTokens);

				foreach (var rtoken in releasedTokens)
				{
					ProcessInferenceToken(rtoken, applyFilter:false);
				}

				tokenFiltered = true;
			}

			StreamingTokenFilter.ReleasedTokens = new();
		}

		return tokenFiltered;
	}

	public virtual void ProcessInferenceToken(string token, bool applyFilter = false)
	{
		
	}

	/*******************
	*  State methods  *
	*******************/

	public async override void _State_InferenceRunning_OnUpdate()
	{
		LoggerManager.LogDebug("Entered InferenceRunning update state");

		try
		{
			await ExecuteInference();
		}
		catch (System.Exception e)
		{
			LoggerManager.LogDebug("Inference exception", "", "e", e.Message);

			InferenceResult.Error = new() {
				Code = "inference_exception",
				Type = e.GetType().Name,
				Message = e.Message,
				Exception = e
			};
		}

		_state.Transition(UNLOAD_MODEL_STATE);
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


	/****************
	*  Exceptions  *
	****************/
	
	public class PromptExceedsContextLengthException : Exception
	{
		public PromptExceedsContextLengthException() { }
		public PromptExceedsContextLengthException(string message) : base(message) { }
		public PromptExceedsContextLengthException(string message, Exception inner) : base(message, inner) { }
		protected PromptExceedsContextLengthException(
			System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
				: base(info, context) { }
	}

	public class FailedLoadingModelException : Exception
	{
		public FailedLoadingModelException() { }
		public FailedLoadingModelException(string message) : base(message) { }
		public FailedLoadingModelException(string message, Exception inner) : base(message, inner) { }
		protected FailedLoadingModelException(
			System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
				: base(info, context) { }
	}
}

