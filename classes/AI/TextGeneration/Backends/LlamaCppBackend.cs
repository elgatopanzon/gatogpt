/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaCpp
 * @created     : Saturday Jan 13, 2024 16:46:53 CST
 */

namespace GatoGPT.AI.TextGeneration.Backends;

using GatoGPT.CLI;
using GatoGPT.Event;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Threading;
using GodotEGP.Misc;

using System.Text.RegularExpressions;
using System.ComponentModel;

public partial class LlamaCppBackend : TextGenerationBackend
{
	private ProcessRunner _processRunner { get; set; }
	private string _command = "llama.cpp";
	private Queue<string> _tokenPrintQueue { get; set; } = new();
	private bool _printQueuePrinting = false;

	public LlamaCppBackend(ModelDefinition modelDefinition, bool isStateful = false) : base(modelDefinition, isStateful)
	{
		ModelDefinition = modelDefinition;

		LoggerManager.LogDebug("Created llamacpp backend", "", "instanceId", InstanceId);
		LoggerManager.LogDebug("", "", "modelDefinition", ModelDefinition);

		_state.Enter();
	}

	public override void StartInference(string promptText, AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null)
	{
		Prompt = promptText;
		CurrentInferenceLine = "";

		Running = true;

		LoggerManager.LogDebug("Starting inference", "", "prompt", Prompt);

		InferenceParams = ModelDefinition.ModelProfile.InferenceParams.DeepCopy();
		LoadParams = ModelDefinition.ModelProfile.LoadParams.DeepCopy();

		if (inferenceParams != null)
		{
			InferenceParams.MergeFrom(inferenceParams);
		}

		// transition to load model state to setup args
		_state.Transition(LOAD_MODEL_STATE);
	}

	public void SetupProcessArgs()
	{
		// convert LoadParams and InferenceParams properties into llama.cpp
		// commands
		
		// load params
		_processRunner.AddArguments("--model", ProjectSettings.GlobalizePath(ModelDefinition.ModelResource.Definition.Path));
		_processRunner.AddArguments("--ctx-size", LoadParams.NCtx.ToString());
		_processRunner.AddArguments("--batch-size", LoadParams.NBatch.ToString());
		_processRunner.AddArguments("--seed", LoadParams.Seed.ToString());
		// _processRunner.AddArguments("--rope-scaling", InferenceParams.RopeScaling.ToString());
		_processRunner.AddArguments("--rope-freq-base", LoadParams.RopeFreqBase.ToString());
		_processRunner.AddArguments("--rope-freq-scale", LoadParams.RopeFreqScale.ToString());

		// vision models / multimodal models
		if (LoadParams.MMProjPath.Length > 0)
		{
			_processRunner.AddArguments("--mmproj", $"\"{ProjectSettings.GlobalizePath(LoadParams.MMProjPath.ToString())}\"");
			_processRunner.AddArguments("--image", $"\"{ProjectSettings.GlobalizePath(InferenceParams.ImagePath.ToString())}\"");

			// switch command to llava
			_processRunner.Command = "llama.cpp-llava-cli";
		}

		if (LoadParams.UseMlock)
		{
			_processRunner.AddArguments("--mlock");
		}
		if (!LoadParams.UseMMap)
		{
			_processRunner.AddArguments("--no-mmap");
		}
		if (!LoadParams.KVOffload)
		{
			_processRunner.AddArguments("--no-kv-offload");
		}

		// inference params
		_processRunner.AddArguments("--prompt", $"\"{GetCurrentPrompt()}\"");
		_processRunner.AddArguments("--escape");
		_processRunner.AddArguments("--threads", InferenceParams.NThreads.ToString());
		_processRunner.AddArguments("--n-predict", InferenceParams.NPredict.ToString());

		_processRunner.AddArguments("--tfs", InferenceParams.Tfs.ToString());
		_processRunner.AddArguments("--typical", InferenceParams.Typical.ToString());
		_processRunner.AddArguments("--repeat-last-n", InferenceParams.RepeatLastN.ToString());
		_processRunner.AddArguments("--repeat-penalty", InferenceParams.RepeatPenalty.ToString());
		_processRunner.AddArguments("--presence-penalty", InferenceParams.PresencePenalty.ToString());
		_processRunner.AddArguments("--frequency-penalty", InferenceParams.FrequencyPenalty.ToString());

		_processRunner.AddArguments("--temp", InferenceParams.Temp.ToString());
		_processRunner.AddArguments("--keep", InferenceParams.KeepTokens.ToString());

		foreach (string antiprompt in InferenceParams.Antiprompts)
		{
			_processRunner.AddArguments("--reverse-prompt", $"\"{antiprompt}\"");
		}

		// TODO: handle prompt cache using LlamaCacheManager
		// _processRunner.AddArguments("--prompt-cache-all");
		// _processRunner.AddArguments("--prompt-cache", "TODO");

		// handle --grammar-file when configured
		if (InferenceParams.GrammarResource != null)
		{
			LoggerManager.LogDebug("Using grammar resource", "", "grammar", InferenceParams.GrammarResourceId);
			_processRunner.AddArguments("--grammar-file", $"\"{ProjectSettings.GlobalizePath(InferenceParams.GrammarResource.Definition.Path)}\"");
		}

		LoggerManager.LogDebug("Llama.cpp args", "", "args", _processRunner.Args);
		Console.WriteLine(String.Join(" ", _processRunner.Args));
	}

	public string GetCurrentPrompt()
	{
		string currentPrompt = Prompt;

		LoggerManager.LogDebug("User prompt", "", "userPrompt", Prompt);

		// TODO: implement state support
		// if ((IsFirstRun && Stateful) || !Stateful)
		// {
			currentPrompt = FormatPrompt(Prompt);

			Console.WriteLine(currentPrompt);
		// }

		return currentPrompt;
	}

	public async void ProcessInferenceLine(string line)
	{
		line = line.Replace(GetCurrentPrompt(), "");
		LoggerManager.LogDebug("Inference line", "", "line", line);

    	if (InferenceResult.GenerationTokenCount == 0)
    	{
    		InferenceResult.FirstTokenTime = DateTime.Now;
    		InferenceResult.PrevTokenTime = DateTime.Now;
    	}

		string[] lineAsTokens = line.Split(" ");
		LoggerManager.LogDebug("Time to first token", "", "time", (InferenceResult.PrevTokenTime - InferenceResult.StartTime).TotalMilliseconds);
		LoggerManager.LogDebug("Tokens in line", "", "tokens", lineAsTokens.Count());
		double printSpeed = (InferenceResult.PrevTokenTime - InferenceResult.StartTime).TotalMilliseconds / lineAsTokens.Count();
		printSpeed = Math.Min(printSpeed, 200);

		LoggerManager.LogDebug("Token print speed", "", "printSpeed", printSpeed);

		// queue up fake tokens
		int count = 1;
		foreach (string fakeToken in lineAsTokens)
		{
			string t = fakeToken;
			if (count < lineAsTokens.Count())
			{
				t+=" ";
			}
			_tokenPrintQueue.Enqueue(t);
			count++;
		}
		_tokenPrintQueue.Enqueue("\n");

		// issue fake tokens from print queue
		if (!_printQueuePrinting)
		{
			while (_tokenPrintQueue.TryPeek(out string t))
			{
				string token = _tokenPrintQueue.Dequeue();

				ProcessInferenceToken(token);

				await Task.Delay(Convert.ToInt32(printSpeed));
			}

			_printQueuePrinting = false;

			// if process exited then go to unload state and end inference
			if (_processRunner.ReturnCode != -1 && _state.CurrentSubState == _inferenceRunningState)
			{
				LoggerManager.LogDebug("Process finished");
				_state.Transition(UNLOAD_MODEL_STATE);
			}
		}
	}

	public void ProcessInferenceToken(string text)
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
	}

	/*******************
	*  State methods  *
	*******************/
	
	public override void _State_Setup_OnEnter()
	{
		// create process runner instance
		_processRunner = new ProcessRunner(_command);

		// add process filter to exclude certain output
		_processRunner.AddOutputFilter((o) => {
			return Regex.IsMatch(o, @"^(llm_|llama_|clip_|encode_|[.]+)");
			});
	}
	public override void _State_LoadModel_OnEnter()
	{
		// no need to load model
		SetupProcessArgs();

		_state.Transition(INFERENCE_RUNNING_STATE);
	}
	// public override void _State_LoadModel_OnUpdate()
	// {
	// }
	public override void _State_UnloadModel_OnEnter()
	{
		_state.Transition(INFERENCE_FINISHED_STATE);
	}
	// public override void _State_UnloadModel_OnUpdate()
	// {
	// }
	public override void _State_InferenceRunning_OnEnter()
	{
		this.Emit<TextGenerationInferenceStart>((o) => {
			o.SetInstanceId(InstanceId);
			});

		InferenceResult = new InferenceResult();

		Run();
	}
	public async override void _State_InferenceRunning_OnUpdate()
	{
		LoggerManager.LogDebug("Starting llama.cpp inference");

		// format the input prompt
		string fullPrompt = GetCurrentPrompt();

		LoggerManager.LogDebug("User prompt", "", "userPrompt", Prompt);
		LoggerManager.LogDebug("Full prompt", "", "fullPrompt", fullPrompt);

		// set fake prompt token count using 100,000 words = 75,000 tokens
		InferenceResult.PromptTokenCount = Convert.ToInt32(fullPrompt.Split(" ").Count() * 0.75);

		// TODO: handle stateful stuff here
		
		// setup process events
		_processRunner.SubscribeOwner<ProcessOutputLine>(_On_ProcessOutputLine);
		_processRunner.SubscribeOwner<ProcessFinishedSuccess>(_On_ProcessFinishedSuccess);
		_processRunner.SubscribeOwner<ProcessFinishedError>(_On_ProcessFinishedError);

		// run and wait for process to exit
		await _processRunner.Execute();
	}
	public override void _State_InferenceFinished_OnEnter()
	{
		LoggerManager.LogDebug("Inference finished");

		// remove trailing \n token
		InferenceResult.Tokens = InferenceResult.Tokens.Take(InferenceResult.Tokens.Count() - 1).ToList();

		InferenceResult.Finished = true;
		Running = false;
		IsFirstRun = false;

		InferenceResult.OutputStripped = FormatOutput(InferenceResult.Output);

		LoggerManager.LogDebug("Llama.cpp output", "", "output", InferenceResult.Output);

		// clear tokens on non-successful exit
		if (!_processRunner.Success)
		{
			InferenceResult.Tokens = new();
		}
		
		this.Emit<TextGenerationInferenceFinished>((o) => {
			o.SetInstanceId(InstanceId);
			o.SetResult(InferenceResult);
			});
	}
	public override void _State_InferenceFinished_OnUpdate()
	{
	}

	/**********************
	*  Callback methods  *
	**********************/
	public void _On_TokenPrintTimer_Timeout(IEvent e)
	{
		LoggerManager.LogDebug("Inference token", "", "token", "");
	}

	public void _On_ProcessOutputLine(ProcessOutputLine e)
	{
		ProcessInferenceLine(e.Line);
	}
	public void _On_ProcessFinishedSuccess(ProcessFinishedSuccess e)
	{
	}
	public void _On_ProcessFinishedError(ProcessFinishedError e)
	{
	}
	
	/***************************
	*  Thread worker methods  *
	***************************/
	
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
}

