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
	private string _promptFilePath { get; set; }
	private string _cfgPromptFilePath { get; set; }

	public Dictionary<string, (string Type, string value)> Metadata { get; set; } = new();

	public LlamaCppBackend(ModelDefinition modelDefinition, bool isStateful = false) : base(modelDefinition, isStateful)
	{
		ModelDefinition = modelDefinition;

		_promptFilePath = ProjectSettings.GlobalizePath($"user://Cache/llama.cpp-prompt-file-{GetHashCode()}");
		_cfgPromptFilePath = ProjectSettings.GlobalizePath($"user://Cache/llama.cpp-cfg-prompt-file-{GetHashCode()}");

		LoggerManager.LogDebug("Created llamacpp backend", "", "instanceId", InstanceId);
		LoggerManager.LogDebug("", "", "modelDefinition", ModelDefinition);

		_state.Enter();
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
		_processRunner.AddArguments("--rope-freq-base", LoadParams.RopeFreqBase.ToString());
		_processRunner.AddArguments("--rope-freq-scale", LoadParams.RopeFreqScale.ToString());

		_processRunner.AddArguments("--n-gpu-layers", LoadParams.NGpuLayers.ToString());

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
		File.WriteAllText(_promptFilePath, GetCurrentPrompt(), System.Text.Encoding.UTF8);
		File.WriteAllText(_cfgPromptFilePath, GetCurrentCfgPrompt(), System.Text.Encoding.UTF8);

		_processRunner.AddArguments("--file", $"\"{_promptFilePath}\"");
		_processRunner.AddArguments("--no-display-prompt");
		_processRunner.AddArguments("--escape");
		_processRunner.AddArguments("--threads", InferenceParams.NThreads.ToString());
		_processRunner.AddArguments("--n-predict", InferenceParams.NPredict.ToString());
		_processRunner.AddArguments("--tfs", InferenceParams.Tfs.ToString());

		_processRunner.AddArguments("--top-k", InferenceParams.TopK.ToString());
		_processRunner.AddArguments("--min-p", InferenceParams.MinP.ToString());
		_processRunner.AddArguments("--top-p", InferenceParams.TopP.ToString());
		_processRunner.AddArguments("--typical", InferenceParams.Typical.ToString());
		_processRunner.AddArguments("--repeat-last-n", InferenceParams.RepeatLastN.ToString());
		_processRunner.AddArguments("--repeat-penalty", InferenceParams.RepeatPenalty.ToString());
		_processRunner.AddArguments("--presence-penalty", InferenceParams.PresencePenalty.ToString());
		_processRunner.AddArguments("--frequency-penalty", InferenceParams.FrequencyPenalty.ToString());

		_processRunner.AddArguments("--mirostat", InferenceParams.Mirostat.ToString());
		_processRunner.AddArguments("--mirostat-lr", InferenceParams.MirostatLearningRate.ToString());
		_processRunner.AddArguments("--mirostat-ent", InferenceParams.MirostatEntropy.ToString());

		_processRunner.AddArguments("--temp", InferenceParams.Temp.ToString());
		_processRunner.AddArguments("--keep", InferenceParams.KeepTokens.ToString());

		_processRunner.AddArguments("--samplers", $"\"{String.Join(";", InferenceParams.Samplers)}\"");

		foreach (string antiprompt in InferenceParams.Antiprompts)
		{
			_processRunner.AddArguments("--reverse-prompt", $"\"{antiprompt.Trim()}\"");
		}

		_processRunner.AddArguments("--cfg-negative-prompt-file", $"\"{_cfgPromptFilePath}\"");
		_processRunner.AddArguments("--cfg-scale", InferenceParams.CfgScale.ToString());

		if (ModelDefinition.PromptCache)
		{
			_processRunner.AddArguments("--prompt-cache-all");
			_processRunner.AddArguments("--prompt-cache", ProjectSettings.GlobalizePath($"user://Cache/llama.cpp-prompt-cache-{InferenceParams.PromptCacheId}-{Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(ModelDefinition.ModelResourceId+LoadParams.NCtx))}"));
		}

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

	public string GetCurrentCfgPrompt()
	{
		return InferenceParams.NegativeCfgPrompt;
	}

	public void ProcessInferenceLine(string token)
{
		LoggerManager.LogDebug("Inference token", "", "token", token);

		ProcessInferenceToken(token);
	}

	public override void ProcessInferenceToken(string text, bool applyFilter = true)
	{
		if (applyFilter && FilterToken(text))
		{
			return;
		}
		// strip the prompt from the output when there's no tokens
		if (InferenceResult.Tokens.Count == 0)
		{
			text = text.Replace(" "+GetCurrentPrompt(), string.Empty);
		}

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

	public async override Task<bool> ExecuteInference()
	{
		LoggerManager.LogDebug("Starting llama.cpp inference");

		// format the input prompt
		string fullPrompt = GetCurrentPrompt();

		VerifyPromptCacheLength();

		LoggerManager.LogDebug("User prompt", "", "userPrompt", Prompt);
		LoggerManager.LogDebug("Full prompt", "", "fullPrompt", fullPrompt);

		Console.WriteLine("");
		Console.WriteLine(fullPrompt);
		Console.WriteLine("");

		// set fake prompt token count using 100,000 words = 75,000 tokens
		InferenceResult.PromptTokenCount = TokenizeString(fullPrompt).Count();

		// TODO: handle stateful stuff here
		
		// setup process events
		_processRunner.SubscribeOwner<ProcessOutputLine>(_On_ProcessOutputLine);
		_processRunner.SubscribeOwner<ProcessFinishedSuccess>(_On_ProcessFinishedSuccess);
		_processRunner.SubscribeOwner<ProcessFinishedError>(_On_ProcessFinishedError);

		// run and wait for process to exit
		await _processRunner.Execute();
		bool success = (_processRunner.ReturnCode == 0);

		if (!success && InferenceResult.Error == null)
		{
			InferenceResult.Error = new InferenceError();
			InferenceResult.Error.Type = "crash";
			InferenceResult.Error.Code = _processRunner.ReturnCode.ToString();
			InferenceResult.Error.Message = $"llama.cpp non-zero return code";
		}

		if (InferenceResult.Error != null)
		{
			LoggerManager.LogDebug("Inference execution error", "", "error", InferenceResult.Error);

			if (InferenceResult.Error.Type == "ErrorLoadingModel")
			{
				throw new FailedLoadingModelException(InferenceResult.Error.Message);
			}
		}

		return success;
	}

	public override List<TokenizedString> TokenizeString(string content, bool skipBos = true)
	{
		var proc = new ProcessRunner("llama.cpp-tokenize");
		proc.AddArguments($"\"{ProjectSettings.GlobalizePath(ModelDefinition.ModelResource.Definition.Path)}\"");
		proc.AddArguments($@"""{content.Replace(@"""", @"\""").Replace("\n", " ")}""");

		List<TokenizedString> tokenizeOutput = new();

		var res = proc.Execute();

		// wait for process result
		while (!proc.IsCompleted)
		{
			
		}

		foreach (var tokenUnparsed in proc.Output.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray())
		{
			var match = Regex.Match(tokenUnparsed, @"([\d]*) -> '(.*)'");
			if (match.Success)
			{
				var token = new TokenizedString() { 
					Id = Convert.ToInt32(match.Groups[1].Value),
					Token = match.Groups[2].Value
				};

				if (token.Id == 1 && skipBos)
				{
					continue;
				}
				tokenizeOutput.Add(token);
			}
		}

		LoggerManager.LogDebug("Tokenized output", "", "tokenizeOutput", tokenizeOutput);

		return tokenizeOutput;
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
			var match = Regex.IsMatch(o, @"^(llm_|llama_|clip_|encode_)");

			// TODO: parse loading metadata values
			string metadataKey = "";
			string metadataType = "";
			string metadataValue = "";

			// llama_model_loader: - kv[ ]*[\d]*:[ ]*([a-z0-9.]*) ([a-z0-9,\[\]]*)[ ]*=[ ]*(.*)
			var metadataMatch = Regex.Match(o, @"llama_model_loader: - kv[ ]*[\d]*:[ ]*([a-z0-9._]*) ([a-z0-9,\[\]]*)[ ]*=[ ]*(.*)");

			if (metadataMatch.Success)
			{
				metadataKey = metadataMatch.Groups[1].Value;
				metadataType = metadataMatch.Groups[2].Value;
				metadataValue = metadataMatch.Groups[3].Value;
			}

			// llm_load_print_meta: ([a-z_]*)[ ]*= (.*)
			metadataMatch = Regex.Match(o, @"llm_load_print_meta: ([a-z_ ]*)[ ]*= (.*)");

			if (metadataMatch.Success)
			{
				metadataKey = metadataMatch.Groups[1].Value.Trim();
				metadataType = "str";
				metadataValue = metadataMatch.Groups[2].Value;
			}

			// llm_load_tensors: offloaded ([0-9]*)/([0-9]*)
			metadataMatch = Regex.Match(o, @"llm_load_tensors: offloaded ([0-9]*)/([0-9]*)");

			if (metadataMatch.Success)
			{
				metadataKey = "custom.gpu_layers";
				metadataType = "u32";
				metadataValue = metadataMatch.Groups[2].Value;
			}

			if (metadataKey.Length > 0)
			{
				Metadata.Add(metadataKey, (metadataType, metadataValue));

				LoggerManager.LogDebug($"Metadata parsed", "", metadataKey, metadataValue);
			}

			return match;
			});

		_processRunner.AddOutputFilter((o) => {
			var match = Regex.IsMatch(o, @"error loading model");

			if (match)
			{
				if (InferenceResult.Error == null)
				{
					InferenceResult.Error = new() {
						Type = "ErrorLoadingModel",
						Code = "error_loading_model",
					};
				}

				InferenceResult.Error.Message += o;
			}

			return match;
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
	public override void _State_InferenceFinished_OnEnter()
	{
		LoggerManager.LogDebug("Inference finished");

		// remove prompt temp file
		if (File.Exists(_promptFilePath))
		{
			File.Delete(_promptFilePath);
		}

		InferenceResult.Finished = true;
		Running = false;
		IsFirstRun = false;

		InferenceResult.OutputStripped = FormatOutput(InferenceResult.Output);

		LoggerManager.LogDebug("Llama.cpp output", "", "output", InferenceResult.Output);
		LoggerManager.LogDebug("Llama.cpp return code", "", "returnCode", _processRunner.ReturnCode);

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
		Console.WriteLine(InferenceResult.Output);
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

