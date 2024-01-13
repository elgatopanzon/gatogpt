/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelInstance
 * @created     : Friday Jan 12, 2024 18:55:55 CST
 */

namespace GatoGPT.AI;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Threading;
using GodotEGP.State;

public partial class ModelBackend : BackgroundJob, AI.IModelBackend
{
	public string InstanceId { get; set; }
	public bool Stateful { get; set; } = false;
	public bool IsFirstRun { get; set; } = true;
	public bool Running { get; set; }
	public AI.ModelDefinition ModelDefinition { get; set; }

	protected const int SETUP_STATE = 0;
	protected const int LOAD_MODEL_STATE = 1;
	protected const int UNLOAD_MODEL_STATE = 2;
	protected const int INFERENCE_RUNNING_STATE = 3;
	protected const int INFERENCE_FINISHED_STATE = 4;

	protected class ModelInstanceState : HStateMachine {};
	protected class SetupState : HStateMachine {};
	protected class LoadModelState : HStateMachine {};
	protected class UnloadModelState : HStateMachine {};
	protected class InferenceRunningState : HStateMachine {};
	protected class InferenceFinishedState : HStateMachine {};

	protected ModelInstanceState _state {get; set;}
	protected SetupState _setupState { get; set; }
	protected LoadModelState _loadModelState { get; set; }
	protected UnloadModelState _unloadModelState { get; set; }
	protected InferenceRunningState _inferenceRunningState { get; set; }
	protected InferenceFinishedState _inferenceFinishedState { get; set; }

	public ModelBackend(AI.ModelDefinition modelDefinition, bool isStateful = false)
	{
		ModelDefinition = modelDefinition;

		SetInstanceId();

		Stateful = isStateful;

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

		LoggerManager.LogDebug("Created instance", "", "modelDefinition", modelDefinition);
	}

	/********************************
	*  Model instance manageement  *
	********************************/
	public void SetInstanceId(string id = "", bool keepState = true)
	{
		if (id == "")
		{
			id = $"{ModelDefinition.Id}-{GetHashCode()}";
		}

		InstanceId = id;
	}

	public virtual void LoadModel()
	{
		
	}
	public virtual void UnloadModel()
	{
		
	}
	public virtual bool SafeToUnloadModel()
	{
		return true;
	}
	public virtual void DeleteInstanceState(bool keepCache = true)
	{
	}

	/*******************
	*  State methods  *
	*******************/
	
	public virtual void _State_Setup_OnEnter()
	{
	}
	public virtual void _State_LoadModel_OnEnter()
	{
	}
	public virtual void _State_LoadModel_OnUpdate()
	{
	}
	public virtual void _State_UnloadModel_OnEnter()
	{
	}
	public virtual void _State_UnloadModel_OnUpdate()
	{
	}
	public virtual void _State_InferenceRunning_OnEnter()
	{
	}
	public virtual void _State_InferenceRunning_OnUpdate()
	{
	}
	public virtual void _State_InferenceFinished_OnEnter()
	{
	}
	public virtual void _State_InferenceFinished_OnUpdate()
	{
	}
}

