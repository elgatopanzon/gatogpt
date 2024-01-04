/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaModelInstance
 * @created     : Tuesday Jan 02, 2024 12:31:11 CST
 */

namespace GatoGPT.LLM;

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

public partial class LlamaModelInstance : BackgroundJob
{
	// used to identify this instance
	private string _instanceId;
	public string InstanceId
	{
		get { return _instanceId; }
		set { _instanceId = value; }
	}

	private string _contextStatePath {
		get {
			return Path.Combine(OS.GetUserDataDir(), InstanceId+"-context");
		}
	}
	private string _executorStatePath {
		get {
			return Path.Combine(OS.GetUserDataDir(), InstanceId+"-executor");
		}
	}

	// holds the definition of the model we are currently working with
	private ModelDefinition _modelDefinition;
	public ModelDefinition ModelDefinition
	{
		get { return _modelDefinition; }
		set { _modelDefinition = value; }
	}

	// LLamaSharp specific properties
	// holds the model params object in LLamaSharp format
	private LLama.Common.ModelParams _modelParams;
	private LLama.Common.InferenceParams _inferenceParams;

	// the loaded weights for the model
	private LLamaWeights _llamaWeights;

	// the LLamaSharp context
	private LLamaContext _llamaContext;

	// the LLamaSharp executor, accepting the created context
	// private StatelessExecutor _executor;
	private InstructExecutor _executorStateful;
	private StatelessExecutor _executor;
	private bool _stateful = false;

	// current text prompt used for inference
	public string Prompt;
	private string _currentInferenceLine = "";

	public InferenceResult InferenceResult { get; set; }

	// DateTime instances used for timing events
	private DateTime _modelLoadStartTime;

	// state machines
	class ModelInstanceState : HStateMachine {};
	class SetupState : HStateMachine {};
	class LoadModelState : HStateMachine {};
	class UnloadModelState : HStateMachine {};
	class InferenceRunningState : HStateMachine {};
	class InferenceFinishedState : HStateMachine {};

	private ModelInstanceState _state {get; set;}
	private SetupState _setupState { get; set; }
	private LoadModelState _loadModelState { get; set; }
	private UnloadModelState _unloadModelState { get; set; }
	private InferenceRunningState _inferenceRunningState { get; set; }
	private InferenceFinishedState _inferenceFinishedState { get; set; }

	private const int SETUP_STATE = 0;
	private const int LOAD_MODEL_STATE = 1;
	private const int UNLOAD_MODEL_STATE = 2;
	private const int INFERENCE_RUNNING_STATE = 3;
	private const int INFERENCE_FINISHED_STATE = 4;

	public LlamaModelInstance(ModelDefinition modelDefinition, bool isStateful = false)
	{
		_modelDefinition = modelDefinition;

		SetInstanceId();

		_stateful = isStateful;

		// setup states
		_state = new();
		_setupState = new();
		_loadModelState = new();
		_unloadModelState = new();
		_inferenceRunningState = new();
		_inferenceFinishedState = new();

		// add states to root state
		_state.AddState(_setupState);
		_state.AddState(_loadModelState);
		_state.AddState(_unloadModelState);
		_state.AddState(_inferenceRunningState);
		_state.AddState(_inferenceFinishedState);

		// add state change callbacks
		_setupState.OnEnter = _State_Setup_OnEnter;
		_loadModelState.OnEnter = _State_LoadModel_OnEnter;
		_loadModelState.OnUpdate = _State_LoadModel_OnUpdate;
		_unloadModelState.OnEnter = _State_UnloadModel_OnEnter;
		_unloadModelState.OnUpdate = _State_UnloadModel_OnUpdate;
		_inferenceRunningState.OnEnter = _State_InferenceRunning_OnEnter;
		_inferenceRunningState.OnUpdate = _State_InferenceRunning_OnUpdate;
		_inferenceFinishedState.OnEnter = _State_InferenceFinished_OnEnter;

		// configure state transitions
		// after setup is finished, we can load the model
		_state.AddTransition(_setupState, _loadModelState, LOAD_MODEL_STATE);

		// with the model loaded, we can begin running the inference loop
		_state.AddTransition(_loadModelState, _inferenceRunningState, INFERENCE_RUNNING_STATE);

		// from the running state we can change to the unload state
		_state.AddTransition(_inferenceRunningState, _unloadModelState, UNLOAD_MODEL_STATE);

		// once model is unloaded inference is considered finished
		_state.AddTransition(_unloadModelState, _inferenceFinishedState, INFERENCE_FINISHED_STATE);

		// if we want to re-run inferrence, we need to restart from loadModel state
		_state.AddTransition(_inferenceFinishedState, _loadModelState, LOAD_MODEL_STATE);

		// // 1. unload the model
		// _state.AddTransition(_inferenceFinishedState, _unloadModelState, UNLOAD_MODEL_STATE);
		// // // 2. re-setup the model if the definition changed
		// // _state.AddTransition(_inferenceFinishedState, _setupState, SETUP_STATE);
		// // _state.AddTransition(_inferenceFinishedState, _loadModelState, LOAD_MODEL_STATE);
        //
		// // from the unload model state we have to re-setup the params before we
		// // load the model again
		// _state.AddTransition(_unloadModelState, _setupState, SETUP_STATE);
		// _state.AddTransition(_unloadModelState, _setupState, INFERENCE_RUNNING_STATE);
		// _state.AddTransition(_unloadModelState, _loadModelState, LOAD_MODEL_STATE);

		// subscribe to thread events
		this.SubscribeOwner<LlamaModelLoadStart>(_On_ModelLoadStart, true);
		this.SubscribeOwner<LlamaModelLoadFinished>(_On_ModelLoadFinished, true);

		this.SubscribeOwner<LlamaInferenceStart>(_On_InferenceStart, true);
		this.SubscribeOwner<LlamaInferenceFinished>(_On_InferenceFinished, true);
		this.SubscribeOwner<LlamaInferenceToken>(_On_InferenceToken, true);
		this.SubscribeOwner<LlamaInferenceLine>(_On_InferenceLine, true);

		// enter the state machine
		_state.Enter();

		LoggerManager.LogDebug("Created model instance", "", "instanceId", _instanceId);
	}

	public void SetInstanceId()
	{
		_instanceId = $"{_modelDefinition.Id}-{GetHashCode()}";
	}

	/***********************************
	*  LLamaSharp management methods  *
	***********************************/
	public void SetupLoadParams()
	{
		// create model params object using path to model file
		_modelParams = new LLama.Common.ModelParams(ModelDefinition.ModelResource.Definition.Path)
		{
			ContextSize = (uint) _modelDefinition.ModelProfile.LoadParams.NCtx,
			MainGpu = _modelDefinition.ModelProfile.LoadParams.MainGpu,
			GpuLayerCount = _modelDefinition.ModelProfile.LoadParams.NGpuLayers,
			Seed = (uint) _modelDefinition.ModelProfile.LoadParams.Seed,
			UseMemorymap = _modelDefinition.ModelProfile.LoadParams.UseMMap,
			UseMemoryLock = _modelDefinition.ModelProfile.LoadParams.UseMlock,
			BatchSize = (uint) _modelDefinition.ModelProfile.LoadParams.NBatch,
			RopeFrequencyBase = (float) _modelDefinition.ModelProfile.LoadParams.RopeFreqBase,
			RopeFrequencyScale = (float) _modelDefinition.ModelProfile.LoadParams.RopeFreqScale,
			UseFp16Memory = _modelDefinition.ModelProfile.LoadParams.F16KV,
		};

		LoggerManager.LogDebug("Setup model params", "", "params", _modelParams);
	}

	public void SetupInferenceParams()
	{
		// create inference params object
		_inferenceParams = new LLama.Common.InferenceParams()
		{
			// TODO: tokens keep from initial prompt
			TokensKeep = _modelDefinition.ModelProfile.InferenceParams.KeepTokens,
			MaxTokens = _modelDefinition.ModelProfile.InferenceParams.NPredict,
			AntiPrompts = _modelDefinition.ModelProfile.InferenceParams.Antiprompts.Concat(new List<String>() { "" }).ToList(),
			TopK = _modelDefinition.ModelProfile.InferenceParams.TopK,
			TopP = (float) _modelDefinition.ModelProfile.InferenceParams.TopP,
			MinP = (float) _modelDefinition.ModelProfile.InferenceParams.MinP,
			Temperature = (float) _modelDefinition.ModelProfile.InferenceParams.Temp,
			RepeatPenalty = (float) _modelDefinition.ModelProfile.InferenceParams.RepeatPenalty,
		};
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

	public void LoadModel()
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

	public void UnloadModel()
	{
		if (_state.CurrentSubState != _unloadModelState)
		{
			_state.Transition(UNLOAD_MODEL_STATE);
		}
	}

	public bool SafeToUnloadModel()
	{
		return (_state.CurrentSubState != _loadModelState && _state.CurrentSubState != _inferenceRunningState);
	}

	public async Task<bool> SaveInstanceState()
	{
		LoggerManager.LogDebug("Saving state to file");

		_llamaContext.SaveState(_contextStatePath);
		await _executorStateful.SaveState(_executorStatePath);

		return true;
	}

	public async Task<bool> LoadInstanceState()
	{
		if (File.Exists(_contextStatePath))
		{
			LoggerManager.LogDebug("Loading context state from file");
			_llamaContext.LoadState(_contextStatePath);
		}


		if (File.Exists(_executorStatePath))
		{
			LoggerManager.LogDebug("Loading executor state from file");
			await _executorStateful.LoadState(_executorStatePath);
		}

		return true;
	}

	public void DeleteInstanceState()
	{
		if (File.Exists(_contextStatePath))
		{
			LoggerManager.LogDebug("Deleting context state file");
			File.Delete(_contextStatePath);
		}


		if (File.Exists(_executorStatePath))
		{
			LoggerManager.LogDebug("Deleting executor state file");
			File.Delete(_executorStatePath);
		}

		if (_llamaWeights != null)
		{
			_llamaWeights.Dispose();
		}
		_llamaWeights = null;
		_modelParams = null;
		_llamaContext = null;
		_executor = null;
		_executorStateful = null;
		_inferenceParams = null;

		GC.Collect();
	}

	/*****************************
	*  Model inference methods  *
	*****************************/
	public void StartInference(string promptText)
	{
		Prompt = promptText;
		_currentInferenceLine = "";

		LoggerManager.LogDebug("Starting inference", "", "prompt", Prompt);

		LoadModel();
	}

	public void RunInference()
	{
		LoggerManager.LogDebug(_state.CurrentSubState.GetType().Name);

		_state.Transition(INFERENCE_RUNNING_STATE);
	}
	
	public string FormatPrompt(string userPrompt)
	{
		var prePromptP = _modelDefinition.ModelProfile.InferenceParams.PrePromptPrefix;
		var prePromptS = _modelDefinition.ModelProfile.InferenceParams.PrePromptSuffix;
		var prePrompt = _modelDefinition.ModelProfile.InferenceParams.PrePrompt;

		var InputP = _modelDefinition.ModelProfile.InferenceParams.InputPrefix;
		var InputS = _modelDefinition.ModelProfile.InferenceParams.InputSuffix;

		return $"{prePromptP}{prePrompt}{prePromptS}{InputP}{userPrompt}{InputS}";
	}

	public bool ProcessInference(string text)
	{
    	if (InferenceResult.TokenCount == 0)
    	{
    		InferenceResult.FirstTokenTime = DateTime.Now;
    		InferenceResult.PrevTokenTime = DateTime.Now;
    	}

    	// calculate token per sec
    	InferenceResult.PrevTokenTime = DateTime.Now;

		this.Emit<LlamaInferenceToken>((o) => {
			o.SetInstanceId(_instanceId);
			o.SetToken(text);
			});

		InferenceResult.AddToken(text);

		// if a new line is obtained, end the current line and emit a Line
		// event
		if (text == "\n")
		{
			this.Emit<LlamaInferenceLine>((o) => {
				o.SetInstanceId(_instanceId);
				o.SetLine(_currentInferenceLine);
				});

			_currentInferenceLine = "";
		}
		else
		{
			// append text to create the line
    		_currentInferenceLine += text;
		}

		// if an empty token is recieved, break out of the inferent loop
    	if (text.Length == 0)
    	{
			this.Emit<LlamaInferenceLine>((o) => {
				o.SetInstanceId(_instanceId);
				o.SetLine(_currentInferenceLine);
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
		string formattedPrompt = FormatPrompt(Prompt);

		LoggerManager.LogDebug("User prompt", "", "userPrompt", Prompt);
		LoggerManager.LogDebug("Full prompt", "", "fullPrompt", formattedPrompt);

		// start the inference loop
		if (_stateful)
		{
			await foreach (var text in _executorStateful.InferAsync(formattedPrompt, _inferenceParams))
    		{
    			if (ProcessInference(text))
    			{
    				break;
    			}
    		}
		}
		else
		{
			await foreach (var text in _executor.InferAsync(formattedPrompt, _inferenceParams))
    		{
    			if (ProcessInference(text))
    			{
    				break;
    			}
    		}
		}

    	return true;
	}

	/*******************
	*  State methods  *
	*******************/
	
	public void _State_Setup_OnEnter()
	{
		LoggerManager.LogDebug("Entered Setup state");

		SetupLoadParams();
		// if (_autorunOnLoad)
		// {
		// 	LoggerManager.LogDebug("Autorun on load enabled");
        //
		// 	_state.Transition(INFERENCE_RUNNING_STATE);
		// }
	}

	public void _State_LoadModel_OnEnter()
	{
		LoggerManager.LogDebug("Entered LoadModel state");

		// run the thread, which will call LoadModel_OnUpdate state
		Run();
	}

	public async void _State_LoadModel_OnUpdate()
	{
		LoggerManager.LogDebug("Entered LoadModel update state");

		// record current time before loading model
		_modelLoadStartTime = DateTime.Now;

		this.Emit<LlamaModelLoadStart>((o) => o.SetInstanceId(_instanceId));

		// load and prepare the model
		if (_llamaWeights == null)
		{
			LoadModelWeights();
		}

		if (_stateful)
		{
			// only create a new context if one doesn't exist
			if (_llamaContext == null)
			{
				CreateModelContext();
			}

			// create new executor instance
			CreateInferenceExecutor();

			// reload the state if it exists
			await LoadInstanceState();
		}
		else {
			CreateStatelessInferenceExecutor();
		}

		this.Emit<LlamaModelLoadFinished>((o) => o.SetInstanceId(_instanceId));

		// after loading start the inference
		_state.Transition(INFERENCE_RUNNING_STATE);
	}

	public void _State_InferenceRunning_OnEnter()
	{
		LoggerManager.LogDebug("Entered InferenceRunning state");

		// run the thread, which will call InferenceRunning_OnUpdate state
		Run();
	}

	public async void _State_InferenceRunning_OnUpdate()
	{
		LoggerManager.LogDebug("Entered InferenceRunning update state");

		await ExecuteInference();

		_state.Transition(UNLOAD_MODEL_STATE);
	}

	public async void _State_UnloadModel_OnEnter()
	{
		LoggerManager.LogDebug("Entered UnloadModel state");

		if (_stateful)
		{
			await SaveInstanceState();
		}

		Run();
	}

	public void _State_UnloadModel_OnUpdate()
	{
		LoggerManager.LogDebug("Entered UnloadModel update state");

		this.Emit<LlamaModelUnloadStart>((o) => o.SetInstanceId(_instanceId));

		_llamaWeights.Dispose();
		_llamaWeights = null;

		if (!_stateful)
		{
			_modelParams = null;
			_llamaContext = null;
		}
		_executor = null;
		_inferenceParams = null;

		GC.Collect();

		this.Emit<LlamaModelUnloadFinished>((o) => o.SetInstanceId(_instanceId));

		// transition to inference finished state after unloading model
		_state.Transition(INFERENCE_FINISHED_STATE);
	}


	public void _State_InferenceFinished_OnEnter()
	{
		LoggerManager.LogDebug("Entered InferenceFinished state");

		GC.Collect();
		
		this.Emit<LlamaInferenceFinished>((o) => {
			o.SetInstanceId(_instanceId);
			o.SetResult(InferenceResult);
			});
	}

	/**********************
	*  Callback methods  *
	**********************/
	
	public void _On_ModelLoadStart(LlamaModelLoadStart e)
	{
		LoggerManager.LogDebug("Model load started", "", "modelPath", _modelParams.ModelPath);
	}

	public void _On_ModelLoadFinished(LlamaModelLoadFinished e)
	{
		LoggerManager.LogDebug("Model load finished", "", "modelPath", _modelParams.ModelPath);
		LoggerManager.LogDebug("", "", "modelLoadTime", DateTime.Now - _modelLoadStartTime);
	}

	public void _On_InferenceStart(LlamaInferenceStart e)
	{
		LoggerManager.LogDebug("Inference started");
	}
	public void _On_InferenceFinished(LlamaInferenceFinished e)
	{
		LoggerManager.LogDebug("Inference finished", "", "result", e.Result);
	}

	public void _On_InferenceToken(LlamaInferenceToken e)
	{
		// LoggerManager.LogDebug("Inference token recieved", "", "token", e.Token);
	}

	public void _On_InferenceLine(LlamaInferenceLine e)
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

