/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaCppServerBackend
 * @created     : Sunday Jan 14, 2024 13:55:59 CST
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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public partial class LlamaCppServerBackend : TextGenerationBackend
{
	private ProcessRunner _processRunner { get; set; }
	private string _command = "llama.cpp-server";

	public LlamaCppServerBackend(ModelDefinition modelDefinition, bool isStateful = false) : base(modelDefinition, isStateful)
	{
		ModelDefinition = modelDefinition;

		Persistent = (ModelDefinition.Persistent != null && ModelDefinition.Persistent > 0);

		LoggerManager.LogDebug("Created llamacpp server backend", "", "instanceId", InstanceId);
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
		if (loadParams != null)
		{
			LoadParams.MergeFrom(loadParams);
		}

		// transition to load model state to setup args
		_state.Transition(LOAD_MODEL_STATE);
	}

	public void SetupProcessArgs()
	{
		// convert LoadParams and InferenceParams properties into llama.cpp
		// commands
		
		// load params
		_processRunner.Args = new string[] {};

		_processRunner.AddArguments("--model", ProjectSettings.GlobalizePath(ModelDefinition.ModelResource.Definition.Path));
		_processRunner.AddArguments("--ctx-size", LoadParams.NCtx.ToString());
		_processRunner.AddArguments("--batch-size", LoadParams.NBatch.ToString());
		// _processRunner.AddArguments("--seed", LoadParams.Seed.ToString());
		// _processRunner.AddArguments("--rope-scaling", InferenceParams.RopeScaling.ToString());
		_processRunner.AddArguments("--rope-freq-base", LoadParams.RopeFreqBase.ToString());
		_processRunner.AddArguments("--rope-freq-scale", LoadParams.RopeFreqScale.ToString());

		_processRunner.AddArguments("--n-gpu-layers", LoadParams.NGpuLayers.ToString());

		// vision models / multimodal models
		if (LoadParams.MMProjPath.Length > 0)
		{
			_processRunner.AddArguments("--mmproj", $"\"{ProjectSettings.GlobalizePath(LoadParams.MMProjPath.ToString())}\"");
			// _processRunner.AddArguments("--image", $"\"{InferenceParams.ImagePath.ToString()}\"");

			// switch command to llava
			// _processRunner.Command = "llama.cpp-llava-cli";
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
		// _processRunner.AddArguments("--prompt", $"\"{GetCurrentPrompt()}\"");
		// _processRunner.AddArguments("--escape");
		// _processRunner.AddArguments("--threads", InferenceParams.NThreads.ToString());
		// _processRunner.AddArguments("--n-predict", InferenceParams.NPredict.ToString());
        //
		// _processRunner.AddArguments("--tfs", InferenceParams.Tfs.ToString());
		// _processRunner.AddArguments("--typical", InferenceParams.Typical.ToString());
		// _processRunner.AddArguments("--repeat-last-n", InferenceParams.RepeatLastN.ToString());
		// _processRunner.AddArguments("--repeat-penalty", InferenceParams.RepeatPenalty.ToString());
		// _processRunner.AddArguments("--presence-penalty", InferenceParams.PresencePenalty.ToString());
		// _processRunner.AddArguments("--frequency-penalty", InferenceParams.FrequencyPenalty.ToString());
        //
		// _processRunner.AddArguments("--temp", InferenceParams.Temp.ToString());
		// _processRunner.AddArguments("--keep", InferenceParams.KeepTokens.ToString());
        //
		// foreach (string antiprompt in InferenceParams.Antiprompts)
		// {
		// 	_processRunner.AddArguments("--reverse-prompt", $"\"{antiprompt}\"");
		// }

		// TODO: handle prompt cache using LlamaCacheManager
		// _processRunner.AddArguments("--prompt-cache-all");
		// _processRunner.AddArguments("--prompt-cache", ProjectSettings.GlobalizePath("user://Cache/llamacppserver-cache"));

		LoggerManager.LogDebug("Llama.cpp server args", "", "args", _processRunner.Args);
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

	public override bool SafeToUnloadModel()
	{
		return Persistent == false;
	}

	// public async void ProcessInferenceLine(string line)
	// {
	// 	line = line.Replace(GetCurrentPrompt(), "");
	// 	LoggerManager.LogDebug("Inference line", "", "line", line);
    //
    // 	if (InferenceResult.GenerationTokenCount == 0)
    // 	{
    // 		InferenceResult.FirstTokenTime = DateTime.Now;
    // 		InferenceResult.PrevTokenTime = DateTime.Now;
    // 	}
    //
	// 	string[] lineAsTokens = line.Split(" ");
	// 	LoggerManager.LogDebug("Time to first token", "", "time", (InferenceResult.PrevTokenTime - InferenceResult.StartTime).TotalMilliseconds);
	// 	LoggerManager.LogDebug("Tokens in line", "", "tokens", lineAsTokens.Count());
	// 	double printSpeed = (InferenceResult.PrevTokenTime - InferenceResult.StartTime).TotalMilliseconds / lineAsTokens.Count();
	// 	printSpeed = Math.Min(printSpeed, 200);
    //
	// 	LoggerManager.LogDebug("Token print speed", "", "printSpeed", printSpeed);
    //
	// 	// queue up fake tokens
	// 	int count = 1;
	// 	foreach (string fakeToken in lineAsTokens)
	// 	{
	// 		string t = fakeToken;
	// 		if (count < lineAsTokens.Count())
	// 		{
	// 			t+=" ";
	// 		}
	// 		_tokenPrintQueue.Enqueue(t);
	// 		count++;
	// 	}
	// 	_tokenPrintQueue.Enqueue("\n");
    //
	// 	// issue fake tokens from print queue
	// 	if (!_printQueuePrinting)
	// 	{
	// 		while (_tokenPrintQueue.TryPeek(out string t))
	// 		{
	// 			string token = _tokenPrintQueue.Dequeue();
    //
	// 			ProcessInferenceToken(token);
    //
	// 			await Task.Delay(Convert.ToInt32(printSpeed));
	// 		}
    //
	// 		_printQueuePrinting = false;
    //
	// 		// if process exited then go to unload state and end inference
	// 		if (_processRunner.ReturnCode != -1 && _state.CurrentSubState == _inferenceRunningState)
	// 		{
	// 			LoggerManager.LogDebug("Process finished");
	// 			_state.Transition(UNLOAD_MODEL_STATE);
	// 		}
	// 	}
	// }

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

	public void ParseLlamaCppServerObject(string serverObj)
	{
		var serverObjDto = JsonConvert.DeserializeObject<ServerObjectDto>(serverObj);
		serverObjDto.Object = JsonConvert.DeserializeObject<Dictionary<string, object>>(serverObj);

		LoggerManager.LogDebug("Llama.cpp server object", "", "serverObj", serverObjDto);

		if (serverObjDto.Message == "model loaded")
		{
			LoggerManager.LogDebug("Llama.cpp server ready");

			_state.Transition(INFERENCE_RUNNING_STATE);
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
		SetupProcessArgs();

		Run();
	}
	public override void _State_LoadModel_OnUpdate()
	{
		// start the server process and load the model
		//
		// TODO: handle stateful stuff here
		//
		// setup process events
		if (IsFirstRun || (Persistent == false))
		{
			_processRunner.SubscribeOwner<ProcessOutputLine>(_On_ProcessOutputLine);
			_processRunner.SubscribeOwner<ProcessFinishedSuccess>(_On_ProcessFinishedSuccess);
			_processRunner.SubscribeOwner<ProcessFinishedError>(_On_ProcessFinishedError);

			// run and wait for process to exit
			_processRunner.Execute();
		}
		else
		{
			_state.Transition(INFERENCE_RUNNING_STATE);
		}
	}

	public override void _State_UnloadModel_OnEnter()
	{
		Run();
	}
	public override void _State_UnloadModel_OnUpdate()
	{
		// skip unloading
		if (Persistent == false)
		{
			LoggerManager.LogDebug("Killing process");

			_processRunner.Kill();
		}

		_state.Transition(INFERENCE_FINISHED_STATE);
	}

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
		LoggerManager.LogDebug("Starting llama.cpp server inference");

		// format the input prompt
		string fullPrompt = GetCurrentPrompt();

		LoggerManager.LogDebug("User prompt", "", "userPrompt", Prompt);
		LoggerManager.LogDebug("Full prompt", "", "fullPrompt", fullPrompt);

		// set fake prompt token count using 100,000 words = 75,000 tokens
		InferenceResult.PromptTokenCount = Convert.ToInt32(fullPrompt.Split(" ").Count() * 0.75);

		// issue server API call
		var inferenceCreateDto = new CompletionCreateDto() {
			Prompt = fullPrompt,
			NKeep = InferenceParams.KeepTokens,
			NPredict = InferenceParams.NPredict,
			TopK = InferenceParams.TopK,
			TopP = InferenceParams.TopP,
			MinP = InferenceParams.MinP,
			Temperature = InferenceParams.Temp,
			RepeatPenalty = InferenceParams.RepeatPenalty,
			FrequencyPenalty = InferenceParams.FrequencyPenalty,
			PresencePenalty = InferenceParams.PresencePenalty,
			TfsZ = InferenceParams.Tfs,
			TypicalP = InferenceParams.Typical,
			Stop = InferenceParams.Antiprompts,
			CachePrompt = ModelDefinition.PromptCache,
		};

		// load the contents of the grammar file if the grammar resource is set
		if (InferenceParams.GrammarResource != null)
		{
			LoggerManager.LogDebug("Using grammar resource", "", "grammar", InferenceParams.GrammarResourceId);
			inferenceCreateDto.Grammar = File.ReadAllText(ProjectSettings.GlobalizePath(InferenceParams.GrammarResource.Definition.Path));
		}

		LoggerManager.LogDebug("Inference create dto", "", "inferenceCreateDto", inferenceCreateDto);

		if (InferenceParams.ImagePath.Length > 0)
		{
			LoggerManager.LogDebug("Adding image to inference", "", "imagePath", InferenceParams.ImagePath);

			inferenceCreateDto.ImageData.Add(new() {
				Id = 1,
				Data = Convert.ToBase64String(File.ReadAllBytes(InferenceParams.ImagePath)),
				});

			inferenceCreateDto.Prompt = "[img-1]"+inferenceCreateDto.Prompt;
		}


		// send dto object to llama.cpp server
		var httpClient = new HttpClient();

		// serialise object to snake case json
		var content = new StringContent(JsonConvert.SerializeObject(inferenceCreateDto, 
        				new JsonSerializerSettings
					{
    					ContractResolver = new DefaultContractResolver() { NamingStrategy = new SnakeCaseNamingStrategy() },
					}), System.Text.Encoding.UTF8, "application/json");

		// send the request to the server and watch for server sent events
		using (var request = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:8080/completion"){ Content = content })
		using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))

		using (var theStream = await response.Content.ReadAsStreamAsync())
		using (var theStreamReader = new StreamReader(theStream))
		{
    		string sseLine = null;

    		while ((sseLine = await theStreamReader.ReadLineAsync()) != null)
    		{
    			// hackily parse server sent events
    			if (sseLine.StartsWith("data: "))
    			{
    				var completionEventDto = JsonConvert.DeserializeObject<CompletionEventDto>(sseLine.Replace("data: ", ""), new JsonSerializerSettings {
    					ContractResolver = new DefaultContractResolver() { NamingStrategy = new SnakeCaseNamingStrategy() }}
					);

    				LoggerManager.LogDebug("Server sent event", "", "completionEventObj", completionEventDto);

    				if (completionEventDto.Stop)
    				{
    					LoggerManager.LogDebug("Inference server reached stop");

    					InferenceResult.PromptTokenCount = completionEventDto.Timings.PromptN;

    					break;
    				}
    				else
    				{
						ProcessInferenceToken(completionEventDto.Content);
    				}
    			}
    		}
		};

		// once inference finishes, proceed to unload
		_state.Transition(UNLOAD_MODEL_STATE);
	}
	public override void _State_InferenceFinished_OnEnter()
	{
		LoggerManager.LogDebug("Inference finished");

		InferenceResult.Finished = true;
		Running = false;
		IsFirstRun = false;

		InferenceResult.OutputStripped = FormatOutput(InferenceResult.Output);

		LoggerManager.LogDebug("Llama.cpp output", "", "output", InferenceResult.Output);
		
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
	public void _On_ProcessOutputLine(ProcessOutputLine e)
	{
		ParseLlamaCppServerObject(e.Line);
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

public partial class ServerObjectDto
{
	public int Timestamp { get; set; }
	public string Level { get; set; }
	public string Message { get; set; }
	public Dictionary<string, object> Object { get; set; }
}

public partial class CompletionCreateDto
{
	public string Prompt { get; set; }
	public double Temperature { get; set; } = 0.8;
	public int TopK { get; set; } = 40;
	public double TopP { get; set; } = 0.95;
	public double MinP { get; set; } = 0.05;
	public int NPredict { get; set; } = -1;
	public int NKeep { get; set; } = -1;
	public bool Stream { get; set; } = true;
	public List<string> Stop { get; set; } = new();
	public double TfsZ { get; set; } = 1.0;
	public double TypicalP { get; set; } = 1.0;
	public double RepeatPenalty { get; set; } = 1.1;
	public int RepeatLastN { get; set; } = 64;
	public bool PenalizeNL { get; set; } = true;
	public double PresencePenalty { get; set; } = 0.0;
	public double FrequencyPenalty { get; set; } = 0.0;
	public string PenaltyPrompt { get; set; } = null;
	public double Mirostat { get; set; } = 0.0;
	public double MirostatTau { get; set; } = 5.0;
	public double MirostatEta { get; set; } = 0.1;
	public string Grammar { get; set; } = null;
	public int Seed { get; set; } = -1;
	public List<ImageDataDto> ImageData { get; set; } = new();
	public bool CachePrompt { get; set; } = true; // TODO: evaluate
}

public partial class ImageDataDto
{
	public string Data { get; set; } = "";
	public int Id { get; set; } = 0;
}

public partial class CompletionEventDto
{
	public string Content { get; set; } = "";
	public int SlotId { get; set; } = 0;
	public bool Multimodal { get; set; } = false;
	public bool Stop { get; set; } = false;
	public bool StoppedEos { get; set; } = false;
	public bool StoppedLimit { get; set; } = false;
	public bool StoppedWord { get; set; } = false;

	public CompletionEventTimingsDto Timings { get; set; } = new();
}

public partial class CompletionEventTimingsDto
{
	public int PredictedN { get; set; }
	public int PromptN { get; set; }
}
