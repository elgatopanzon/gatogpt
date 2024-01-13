/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : TextGeneration.Backends.Builtin
 * @created     : Tuesday Jan 02, 2024 12:31:11 CST
 */

namespace GatoGPT.AI.TextGeneration.Backends;

using GatoGPT.Event;

using System;
using System.ComponentModel;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Threading;
using GodotEGP.State;

using LLama;
using LLama.Common;

public partial class BuiltinLlamaBackend : AI.TextGeneration.Backends.TextGenerationBackend
{
	private LlamaCacheManager _cacheManager { get; set; }

	// LLamaSharp specific properties
	// holds the model params object in LLamaSharp format
	private LLama.Common.ModelParams _modelParams;
	private LLama.Common.InferenceParams _inferenceParams;

	// the loaded weights for the model
	private LLamaWeights _llamaWeights;

	// the LLamaSharp context
	private LLamaContext _llamaContext;

	// the LLamaSharp executor, accepting the created context
	private InstructExecutor _executorStateful;
	private StatelessExecutor _executor;

	public BuiltinLlamaBackend(ModelDefinition modelDefinition, bool isStateful = false) : base(modelDefinition, isStateful)
	{
		ModelDefinition = modelDefinition;

		// subscribe to thread events
		this.SubscribeOwner<LlamaModelLoadStart>(_On_ModelLoadStart, true);
		this.SubscribeOwner<LlamaModelLoadFinished>(_On_ModelLoadFinished, true);

		// enter the state machine
		_state.Enter();

		LoggerManager.LogDebug("Created llama model instance", "", "instanceId", InstanceId);
		LoggerManager.LogDebug("", "", "modelDefinition", ModelDefinition);
	}

	/***********************************
	*  LLamaSharp management methods  *
	***********************************/
	public void SetupLoadParams()
	{
		// create model params object using path to model file
		_modelParams = new LLama.Common.ModelParams(ModelDefinition.ModelResource.Definition.Path)
		{
			ContextSize = (uint) LoadParams.NCtx,
			MainGpu = LoadParams.MainGpu,
			GpuLayerCount = LoadParams.NGpuLayers,
			Seed = (uint) LoadParams.Seed,
			UseMemorymap = LoadParams.UseMMap,
			UseMemoryLock = LoadParams.UseMlock,
			BatchSize = (uint) LoadParams.NBatch,
			RopeFrequencyBase = (float) LoadParams.RopeFreqBase,
			RopeFrequencyScale = (float) LoadParams.RopeFreqScale,
			// UseFp16Memory = LoadParams.F16KV,
			Threads = (uint) InferenceParams.NThreads,
			NoKqvOffload = (!(LoadParams.KVOffload) || Stateful),
		};

		LoggerManager.LogDebug("Setup model params", "", "params", _modelParams);
	}

	public void SetupInferenceParams()
	{
		// create inference params object
		_inferenceParams = new LLama.Common.InferenceParams()
		{
			// TODO: tokens keep from initial prompt
			TokensKeep = InferenceParams.KeepTokens,
			MaxTokens = InferenceParams.NPredict,
			AntiPrompts = InferenceParams.Antiprompts,
			TopK = InferenceParams.TopK,
			TopP = (float) InferenceParams.TopP,
			MinP = (float) InferenceParams.MinP,
			Temperature = (float) InferenceParams.Temp,
			RepeatPenalty = (float) InferenceParams.RepeatPenalty,
			FrequencyPenalty = (float) InferenceParams.FrequencyPenalty,
			PresencePenalty = (float) InferenceParams.PresencePenalty,
		};

		LoggerManager.LogDebug("Setup inference params", "", "params", _inferenceParams);
	}

	public void LoadModelWeights()
	{
		if (_llamaWeights == null)
		{
			LoggerManager.LogDebug("Loading llama model weights", "", "modelPath", _modelParams.ModelPath);

			_llamaWeights = LLamaWeights.LoadFromFile(_modelParams);

			return;
		}
		
		throw new ModelWeightsAlreadyLoadedException($"Model weights are already loaded, and model must be unloaded first");
	}

	public void CreateModelContext()
	{
		LoggerManager.LogDebug("Creating model context", "", "modelPath", _modelParams.ModelPath);

		_llamaContext = _llamaWeights.CreateContext(_modelParams);
	}

	public void CreateInferenceExecutor()
	{
		LoggerManager.LogDebug("Creating inference executor with context", "", "modelPath", _modelParams.ModelPath);

		_executorStateful = new InstructExecutor(_llamaContext);
	}

	public void CreateStatelessInferenceExecutor()
	{
		LoggerManager.LogDebug("Creating stateless inference executor", "", "modelPath", _modelParams.ModelPath);

		_executor = new StatelessExecutor(_llamaWeights, _modelParams);
	}

	public override void LoadModel()
	{
		if (_llamaWeights != null)
		{
			_llamaWeights.Dispose();
		}
		_llamaWeights = null;
		_executor = null;
		_executorStateful = null;
		_llamaContext = null;
		_inferenceParams = null;

		GC.Collect();

		_state.Transition(LOAD_MODEL_STATE);
	}

	public override void UnloadModel()
	{
		// _state.Transition(UNLOAD_MODEL_STATE);
		ProcessUnloadModel();
	}

	public void ProcessUnloadModel()
	{
		LoggerManager.LogDebug("Unloading model");

		this.Emit<LlamaModelUnloadStart>((o) => o.SetInstanceId(InstanceId));

		if (_llamaWeights != null)
		{
			_llamaWeights.Dispose();
			_llamaWeights.NativeHandle.Close();
			_llamaWeights.NativeHandle.Dispose();
		}
		_llamaWeights = null;

		if (!Stateful)
		{
			_modelParams = null;
			if (_llamaContext != null)
			{
				_llamaContext.Dispose();
				_llamaContext.NativeHandle.Close();
				_llamaContext.NativeHandle.Dispose();
			}
			_llamaContext = null;
		}
		_executor = null;
		_inferenceParams = null;

		GC.Collect();

		this.Emit<LlamaModelUnloadFinished>((o) => o.SetInstanceId(InstanceId));
	}

	public override bool SafeToUnloadModel()
	{
		return (_state.CurrentSubState != _loadModelState && _state.CurrentSubState != _inferenceRunningState);
	}

	public override void DeleteInstanceState(bool keepCache = true)
	{
		if (!keepCache)
		{
			_cacheManager.DeleteCache();
		}

		if (_llamaWeights != null)
		{
			_llamaWeights.Dispose();
		}
		_llamaWeights = null;
		_modelParams = null;

		if (_llamaContext != null)
		{
			_llamaContext.Dispose();
		}
		_llamaContext = null;
		_executor = null;
		_executorStateful = null;
		_inferenceParams = null;

		GC.Collect();
	}

	/*****************************
	*  Model inference methods  *
	*****************************/
	public override void StartInference(string promptText, AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null)
	{
		Prompt = promptText;
		CurrentInferenceLine = "";

		Running = true;

		LoggerManager.LogDebug("Starting inference", "", "prompt", Prompt);


		InferenceParams = ModelDefinition.ModelProfile.InferenceParams.DeepCopy();
		LoadParams = ModelDefinition.ModelProfile.LoadParams.DeepCopy();

		// if we parsed any inference params, merge them into the copy of the
		// model profile's ones
		if (loadParams != null)
		{
			LoggerManager.LogDebug("Load params before", "", "loadParams", LoadParams);
			LoggerManager.LogDebug("Load params before from", "", "loadParams", loadParams);
			LoadParams.MergeFrom(loadParams);
			LoggerManager.LogDebug("Load params after", "", "loadParams", LoadParams);
		}
		if (inferenceParams != null)
		{
			InferenceParams.MergeFrom(inferenceParams);
		}

		LoadModel();
	}

	public void RunInference()
	{
		LoggerManager.LogDebug(_state.CurrentSubState.GetType().Name);

		_state.Transition(INFERENCE_RUNNING_STATE);
	}
	
	public string FormatPrompt(string userPrompt)
	{
		var prePromptP = InferenceParams.PrePromptPrefix;
		var prePromptS = InferenceParams.PrePromptSuffix;
		var prePrompt = InferenceParams.PrePrompt;

		var InputP = InferenceParams.InputPrefix;
		var InputS = InferenceParams.InputSuffix;

		return $"{prePromptP}{prePrompt}{prePromptS}{InputP}{userPrompt}{InputS}";
	}

	public string GetCurrentPrompt()
	{
		string currentPrompt = Prompt;

		LoggerManager.LogDebug("User prompt", "", "userPrompt", Prompt);

		if ((IsFirstRun && Stateful) || !Stateful)
		{
			currentPrompt = FormatPrompt(Prompt);

			Console.WriteLine(currentPrompt);
		}

		return currentPrompt;
	}

	public bool ProcessInference(string text)
	{
    	if (InferenceResult.GenerationTokenCount == 0)
    	{
    		InferenceResult.FirstTokenTime = DateTime.Now;
    		InferenceResult.PrevTokenTime = DateTime.Now;
    	}

    	// calculate token per sec
    	InferenceResult.PrevTokenTime = DateTime.Now;

		this.Emit<TextGenerationInferenceToken>((o) => {
			o.SetInstanceId(InstanceId);
			o.SetToken(text);
			});

		InferenceResult.AddToken(text);

		// if a new line is obtained, end the current line and emit a Line
		// event
		if (text == "\n")
		{
			this.Emit<TextGenerationInferenceLine>((o) => {
				o.SetInstanceId(InstanceId);
				o.SetLine(CurrentInferenceLine);
				});

			CurrentInferenceLine = "";
		}
		else
		{
			// append text to create the line
    		CurrentInferenceLine += text;
		}

		// if an empty token is recieved, break out of the inferent loop
    	if (text.Length == 0)
    	{
			this.Emit<TextGenerationInferenceLine>((o) => {
				o.SetInstanceId(InstanceId);
				o.SetLine(CurrentInferenceLine);
				});

    		return true;
    	}

    	return false;
	}

	public async Task<bool> ExecuteInference()
	{

		// set the inference start time
		InferenceResult = new InferenceResult();

		// format the input prompt
		string fullPrompt = GetCurrentPrompt();

		LoggerManager.LogDebug("User prompt", "", "userPrompt", Prompt);
		LoggerManager.LogDebug("Full prompt", "", "fullPrompt", fullPrompt);

		SetupInferenceParams();

		// re-calculate tokens per second in case it was altered by cache
		if (fullPrompt.Length > 0)
		{
			InferenceResult.PromptTokenCount = _llamaWeights.NativeHandle.Tokenize(fullPrompt, true, false, System.Text.Encoding.UTF8).Count();
		}

		// start the inference loop
		if (Stateful)
		{
			// load prompt cache and get adjusted prompt string
			fullPrompt = await _cacheManager.GetCachedPrompt(fullPrompt, InferenceParams, _llamaContext, _executorStateful);


			LoggerManager.LogDebug("Full prompt after cache", "", "fullPrompt", fullPrompt);

			// create the state with nPredict = 0 to not predict anything and
			// use this as a pre-generation state
			// why? allows us to issue the same prompt and it will act as a
			// re-generation except from cache
			var maxTokens = _inferenceParams.MaxTokens;
			_inferenceParams.MaxTokens = 0;

			await foreach (var text in _executorStateful.InferAsync(fullPrompt, _inferenceParams))
    		{
    			LoggerManager.LogDebug("Pre-state token generated", "", "preStateToken", text);
    		}

			// save prompt cache pre-generation
			await _cacheManager.SavePromptCache(fullPrompt, _llamaContext, _executorStateful);

			_inferenceParams.MaxTokens = maxTokens;

			// generate without passing a prompt (the prompt should be in the
			// context already)
			await foreach (var text in _executorStateful.InferAsync(" ", _inferenceParams))
    		{
    			if (ProcessInference(text))
    			{
    				break;
    			}
    		}

			// // save prompt cache including the generated output
			string promptPrev = Prompt;
			Prompt += InferenceResult.Output;
			string combinedPrompt = GetCurrentPrompt();
			Prompt = promptPrev;
			await _cacheManager.SavePromptCache(combinedPrompt, _llamaContext, _executorStateful);
		}
		else
		{
			await foreach (var text in _executor.InferAsync(fullPrompt, _inferenceParams))
    		{
    			if (ProcessInference(text))
    			{
    				break;
    			}
    		}
		}

    	return true;
	}

	/***************************
	*  Cache manager methods  *
	***************************/
	public void InitCacheManager()
	{
		_cacheManager = new LlamaCacheManager(_modelParams.ContextSize, _modelParams.RopeFrequencyBase, _modelParams.RopeFrequencyScale, ModelDefinition.Id, ModelDefinition.ModelResource.Definition.FileHash);
	}
	

	/*******************
	*  State methods  *
	*******************/
	
	public override void _State_Setup_OnEnter()
	{
		LoggerManager.LogDebug("Entered Setup state");
	}

	public override void _State_LoadModel_OnEnter()
	{
		LoggerManager.LogDebug("Entered LoadModel state");

		// run the thread, which will call LoadModel_OnUpdate state
		Run();
	}

	public async override void _State_LoadModel_OnUpdate()
	{
		LoggerManager.LogDebug("Entered LoadModel update state");

		SetupLoadParams();

		this.Emit<LlamaModelLoadStart>((o) => o.SetInstanceId(InstanceId));

		// load and prepare the model
		if (_llamaWeights == null)
		{
			LoadModelWeights();
		}

		if (Stateful)
		{
			InitCacheManager();

			// only create a new context if one doesn't exist
			if (_llamaContext == null)
			{
				CreateModelContext();
			}

			// create new executor instance
			CreateInferenceExecutor();
		}
		else {
			CreateStatelessInferenceExecutor();
		}

		this.Emit<LlamaModelLoadFinished>((o) => o.SetInstanceId(InstanceId));

		// after loading start the inference
		_state.Transition(INFERENCE_RUNNING_STATE);
	}

	public override void _State_InferenceRunning_OnEnter()
	{
		LoggerManager.LogDebug("Entered InferenceRunning state");

		this.Emit<TextGenerationInferenceStart>((o) => {
			o.SetInstanceId(InstanceId);
			});

		// run the thread, which will call InferenceRunning_OnUpdate state
		Run();
	}

	public async override void _State_InferenceRunning_OnUpdate()
	{
		LoggerManager.LogDebug("Entered InferenceRunning update state");

		await ExecuteInference();

		_state.Transition(UNLOAD_MODEL_STATE);
	}

	public async override void _State_UnloadModel_OnEnter()
	{
		LoggerManager.LogDebug("Entered UnloadModel state");

		Run();
	}

	public override void _State_UnloadModel_OnUpdate()
	{
		LoggerManager.LogDebug("Entered UnloadModel update state");

		ProcessUnloadModel();

		// transition to inference finished state after unloading model
		_state.Transition(INFERENCE_FINISHED_STATE);
	}


	public override void _State_InferenceFinished_OnEnter()
	{
		LoggerManager.LogDebug("Entered InferenceFinished state");

		GC.Collect();

		InferenceResult.Finished = true;
		Running = false;
		IsFirstRun = false;
		
		this.Emit<TextGenerationInferenceFinished>((o) => {
			o.SetInstanceId(InstanceId);
			o.SetResult(InferenceResult);
			});
	}

	/**********************
	*  Callback methods  *
	**********************/
	
	public override void _On_ModelLoadStart(LlamaModelLoadStart e)
	{
		LoggerManager.LogDebug("Model load started", "", "modelPath", _modelParams.ModelPath);
	}

	public override void _On_ModelLoadFinished(LlamaModelLoadFinished e)
	{
		LoggerManager.LogDebug("Model load finished", "", "modelPath", _modelParams.ModelPath);
	}

	public override void _On_InferenceStart(TextGenerationInferenceStart e)
	{
		LoggerManager.LogDebug("Inference started");
	}
	public override void _On_InferenceFinished(TextGenerationInferenceFinished e)
	{
		LoggerManager.LogDebug("Inference finished", "", "result", e.Result);
	}

	public override void _On_InferenceToken(TextGenerationInferenceToken e)
	{
		// LoggerManager.LogDebug("Inference token recieved", "", "token", e.Token);
	}

	public override void _On_InferenceLine(TextGenerationInferenceLine e)
	{
		LoggerManager.LogDebug("Inference line generated", "", "line", e.Line);
	}

	/**********************
	*  Threaded work methods  *
	**********************/

	// load the model / run the inference
	public override void DoWork(object sender, DoWorkEventArgs e)
	{
		_state.Update();
	}

	// report progress (inference tokens)
	public override void ProgressChanged(object sender, ProgressChangedEventArgs e)
	{
		LoggerManager.LogDebug("Run thread progress", "", "state", _state.CurrentSubState.GetType().Name);
	}

	// inference finished
	public override void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		LoggerManager.LogDebug("Run thread completed", "", "state", _state.CurrentSubState.GetType().Name);
	}

	// error during loading / inference
	public override void RunWorkerError(object sender, RunWorkerCompletedEventArgs e)
	{
		LoggerManager.LogDebug("Run thread error", "", "state", _state.CurrentSubState.GetType().Name);

		Running = false;
	}

	/****************
	*  Exceptions  *
	****************/
	public class ModelWeightsAlreadyLoadedException : Exception
	{
		public ModelWeightsAlreadyLoadedException() { }
		public ModelWeightsAlreadyLoadedException(string message) : base(message) { }
		public ModelWeightsAlreadyLoadedException(string message, Exception inner) : base(message, inner) { }
		protected ModelWeightsAlreadyLoadedException(
			System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
				: base(info, context) { }
	}
}

