/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CodeTesting
 * @created     : Saturday Jan 06, 2024 20:18:48 CST
 */

namespace GatoGPT;

using GatoGPT.Service;
using GatoGPT.AI.TextGeneration;
using GatoGPT.Config;
using GatoGPT.CLI;
using GatoGPT.AI.TextGeneration.TokenFilter;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.DAL;
using GodotEGP.DAL.Endpoints;
using GodotEGP.DAL.Operations;
using GodotEGP.DAL.Operators;
using GodotEGP.Resource;

using ChromaDBSharp.Client;
using ChromaDBSharp.Embeddings;
using ChromaDBSharp.Models;
using Newtonsoft.Json;
using System.Net.Http;

using System.Diagnostics;
using System.Text.RegularExpressions;

public partial class CodeTesting
{
	private string[] _args { get; set; }

	private StreamingTokenFilter _tokenFilter { get; set; }
	private List<string> _passedTokens { get; set; } = new();

	public CodeTesting(string[] args)
	{
		_args = args;

		_tokenFilter = new StreamingTokenFilter();
	}

	class TestTuneResult {
		public int SpeedGpuLayers { get; set; }
		public int SpeedThreads { get; set; }
		public double SpeedTokensPerSec { get; set; }

		public List<(int Ctx, int GpuLayers, int Threads, double TokensPerSec)> ContextResults { get; set; } = new();
	}

	class TestTuneState {
		public int State { get; set; } = 0;
		public int Counter { get; set; } = 0;
		public int TuneTarget { get; set; } = 0;

		public int Threads { get; set; } = 0;
		public int PrevThreads { get; set; } = 0;
		public int GpuLayers { get; set; } = 0;
		public int PrevGpuLayers { get; set; } = 0;
		public int Ctx { get; set; } = 0;
		public int PrevCtx { get; set; } = 0;
		public List<double> TokensPerSecDiffAvg { get; set; } = new();
		public List<double> TokensPerSecDiffAvgAvg { get; set; } = new();

		public InferenceResult Result { get; set; }
		public InferenceResult PrevResult { get; set; }

		public string Action { get; set; } = "";
	}

	public async Task<int> Run()
	{
		LoggerManager.LogDebug("Testing class!");

		if (_args.Contains("--tune"))
		{
			string tuneModelId = _args[1];
			int tuneGpuLayersMax = 0;
			LoggerManager.LogDebug("Tuning model parameters", "", "model", tuneModelId);

			int tuneRequiredCtxMin = 64;
			int tuneRequiredCtx = tuneRequiredCtxMin;
			int tuneCtxMax = 0;
			int tuneTargetPredict = 30;

			// load the text content for the tests
			string promptString = "";
			using (HttpClient client = new HttpClient())
			{
    			// promptString = await client.GetStringAsync("https://0x0.st/s/uIJ87ff3E-jb97feriuAPQ/Xrtt.txt");
    			promptString = await client.GetStringAsync("https://0x0.st/s/RBuFkjriNPdMEeJzzmAeNA/Xr5D.txt");
			}

			var textGenService = ServiceRegistry.Get<TextGenerationService>();

			List<TokenizedString> tokenized = textGenService.TokenizeString(tuneModelId, promptString);

			LoggerManager.LogDebug("Test prompt token length", "", "tokens", tokenized.Count);

			var modelManager = ServiceRegistry.Get<TextGenerationModelManager>();

			// tune
			var tuneState = new TestTuneState();
			tuneState.Ctx = tuneRequiredCtx;
			tuneState.PrevCtx = tuneRequiredCtx;

			var tuneResult = new TestTuneResult();

			if (_args.Count() >= 3)
			{
				tuneState.GpuLayers = Convert.ToInt32(_args[2]);
			}
			if (_args.Count() >= 4)
			{
				tuneState.Threads = Convert.ToInt32(_args[3]);
			}
			if (_args.Count() >= 5)
			{
				tuneState.TuneTarget = Convert.ToInt32(_args[4]);

				tuneResult.SpeedThreads = tuneState.Threads;
				tuneResult.SpeedGpuLayers = tuneState.GpuLayers;
			}

			LoggerManager.LogInfo("Starting tune!", "", "tuneModel", tuneModelId);
			LoggerManager.LogInfo("Tune CTX requirement", "", "tuneCtx", tuneRequiredCtx);
			LoggerManager.LogInfo("Tune Token samples (predict)", "", "tunePredict", tuneTargetPredict);

			bool metadataSet = false;
			while (true)
			{
				// 0 = tune threads
				if (tuneState.State == 0)
				{
					LoggerManager.LogInfo("Tuning CPU threads", "", "threads", tuneState.Threads);
				}

				AI.TextGeneration.LoadParams loadParams = modelManager.GetModelDefinition(tuneModelId).ModelProfile.LoadParams.DeepCopy();
				AI.TextGeneration.InferenceParams inferenceParams = modelManager.GetModelDefinition(tuneModelId).ModelProfile.InferenceParams.DeepCopy();

				loadParams.NGpuLayers = tuneState.GpuLayers;
				loadParams.NCtx = tuneState.Ctx;
				loadParams.Seed = 1337;

				inferenceParams.NThreads = tuneState.Threads;
				inferenceParams.NPredict = tuneTargetPredict;
				inferenceParams.PrePrompt = "";
				inferenceParams.PrePromptPrefix = "";
				inferenceParams.PrePromptSuffix = "";
				inferenceParams.InputPrefix = "";
				inferenceParams.InputSuffix = "";
				inferenceParams.Antiprompts = new();

				var modelInstance = textGenService.CreateModelInstance(tuneModelId, false, "");

				var inferenceResult = await textGenService.InferAsync(tuneModelId, String.Join("", tokenized.Take((tuneRequiredCtx-1) - tuneTargetPredict).Select(x => x.Token).ToList()), existingInstanceId:modelInstance.InstanceId, stateful:false, loadParams:loadParams, inferenceParams:inferenceParams);

				tuneState.Result = inferenceResult;
				if (tuneState.PrevResult == null)
				{
					tuneState.PrevResult = inferenceResult;
				}

				// set ctx and gpu layer counts
				if (modelInstance.Metadata.TryGetValue("n_ctx_train", out var modelCtx))
				{
					if (!metadataSet)
					{
						tuneCtxMax = Convert.ToInt32(modelCtx.Value);

						if (tuneState.TuneTarget == 1)
						{
							tuneState.Ctx = tuneCtxMax;
							tuneRequiredCtx = tuneCtxMax;
						}
					}
				}
				if (modelInstance.Metadata.TryGetValue("custom.gpu_layers", out var modelGpuLayers))
				{
					if (!metadataSet)
					{
						tuneGpuLayersMax = Convert.ToInt32(modelGpuLayers.Value);
						tuneState.GpuLayers = tuneGpuLayersMax;
					}
				}
				// assume the above properties are found, then set metadata as
				// set
				if (!metadataSet)
				{
					metadataSet = true;

					tuneState.Threads = 1;

					// if the initial run fails with gpu layers 0 and threads 0,
					// then there's not enough for basic speed tuning at min ctx
					if ((!tuneState.Result.Success || tuneState.Result.OutputStripped.Length == 0))
					{
						LoggerManager.LogDebug("Tune failed: insufficient resources for model");
						break;
					}

					continue;
				}

				LoggerManager.LogDebug("Inference result", "", "inferenceResult", inferenceResult);

				LoggerManager.LogDebug("Tune state", "", "stateId", tuneState.State);
				LoggerManager.LogDebug("Tune target", "", "tuneTarget", tuneState.TuneTarget);
				LoggerManager.LogDebug("Tune run success", "", "success", tuneState.Result.Success);
				if (!tuneState.Result.Success)
				{
					LoggerManager.LogDebug("Tune run error", "", "tuneError", inferenceResult.Error.Message);
				}

				LoggerManager.LogDebug("Tune state threads", "", "threads", tuneState.Threads);
				LoggerManager.LogDebug("Tune state gpu layers", "", "gpuLayers", $"{tuneState.GpuLayers} / {tuneGpuLayersMax}");
				LoggerManager.LogDebug("Tune state ctx", "", "ctx", $"{tuneState.Ctx} / {tuneCtxMax}");
				LoggerManager.LogDebug("Tune state tok/s", "", "toks", tuneState.Result.TokensPerSec);

				// tune state actions
				double tokensPerSecDiff = tuneState.Result.TokensPerSec - tuneState.PrevResult.TokensPerSec;
				int tuneThreadsDiff = tuneState.Threads - tuneState.PrevThreads;
				int tuneCtxDiff = tuneState.Ctx - tuneState.PrevCtx;
				int tuneGpuLayersDiff = tuneState.GpuLayers - tuneState.PrevGpuLayers;

				tuneState.TokensPerSecDiffAvg.Add(tokensPerSecDiff);

				// 0 = gpu layers
				if (tuneState.State == 0)
				{
					if (tuneState.Counter >= 0)
					{
						// assume that failing to infer means running out of
						// memory on the gpu
						// no changes = max layers reached (so threads to 0)
						if ((!tuneState.Result.Success || tuneState.Result.OutputStripped.Length == 0) && tuneState.GpuLayers != 0)
						{
							tuneState.GpuLayers--;
							tuneState.Action = "infer failed (out of resources?)";

							// this means the fastest is the combo of
							// threads + gpu layers, so next step is figuring
							// out the thread count to use with the found gpu
							// layers value
							tuneState.TokensPerSecDiffAvgAvg = new();

							if (tuneState.TuneTarget == 0)
							{
								tuneResult.SpeedGpuLayers = tuneState.GpuLayers;
								tuneResult.SpeedThreads = tuneState.Threads;

								if (tuneState.GpuLayers <= 0)
								{
									// set gpu layers to 0, move on to threads tuning
									tuneState.GpuLayers = 0;
									tuneState.State++;
								}
							}
							else if (tuneState.TuneTarget == 1)
							{
								if (tuneState.GpuLayers <= 0)
								{
									// set gpu layers to 0, skip threads tuning
									tuneState.GpuLayers = 0;
									tuneState.State++;
									tuneState.State++;
								}
							}
						}
						else if (tuneState.GpuLayers == tuneGpuLayersMax)
						{

							// this means that threads can be at 1, because the
							// full load is on the GPU

							if (tuneState.TuneTarget == 0)
							{
								tuneResult.SpeedGpuLayers = tuneState.GpuLayers;
								tuneResult.SpeedThreads = 1;

								tuneState.Action = $"speed: max gpu layers reached ({tuneState.GpuLayers})";

								// reset gpu layers so we can find threads count
								tuneState.GpuLayers = 0;
							}
							else if (tuneState.TuneTarget == 1)
							{
								// if we can run the largest context at the max
								// layers, then we accept that
								tuneState.Action = $"context: max gpu layers reached ({tuneState.GpuLayers})";
								tuneState.State++;
							}

							tuneState.State++;

							tuneState.TokensPerSecDiffAvgAvg = new();
						}
						else
						{
							if (tuneState.TuneTarget == 0)
							{
								tuneState.Action = "speed: non-crashing layer count found";

								tuneState.State++;
							}
							else if (tuneState.TuneTarget == 1)
							{
								// accept first non-crashing layer count in
								// context size tune mode
								tuneState.Action = "context: non-crashing layer count found";
								tuneState.State++;
								tuneState.State++;
							}
						}

						tuneState.Counter = 0;
						tuneState.TokensPerSecDiffAvgAvg.Add(tuneState.TokensPerSecDiffAvg.Average());
					}
					else
					{
						tuneState.Action = $"gpu layers {tuneState.GpuLayers} count {tuneState.Counter + 1}";
						tuneState.Counter++;
					}

				}


				// 1 = threads
				else if (tuneState.State == 1)
				{
					// if the new tokens/s is slower than the current, then
					// reverse the threads increase to the previous one and move
					// to the next state

					if (tuneState.Counter >= (tuneState.TuneTarget == 0 ? 2 : 0))
					{
						if (tuneState.TokensPerSecDiffAvg.Average() < -0.05 && tuneState.Threads != 1)
						{
							tuneState.Threads--;

							tuneState.State++;
							tuneState.TokensPerSecDiffAvgAvg = new();

							if (tuneState.TuneTarget == 0)
							{
								tuneResult.SpeedThreads = tuneState.Threads;
								tuneState.Action = "speed: revert threads to prev value";

								// restore speed gpu layers value
								tuneState.GpuLayers = tuneResult.SpeedGpuLayers;
							}
						}
						else
						{
							tuneState.Action = "speed: increase thread count";
							tuneState.Threads++;
						}

						tuneState.Counter = 0;
						tuneState.TokensPerSecDiffAvgAvg.Add(tuneState.TokensPerSecDiffAvg.Average());
					}
					else
					{
						tuneState.Action = $"speed: threads {tuneState.Threads} count {tuneState.Counter + 1}";
						tuneState.Counter++;
					}

				}

				if (tuneState.State == 2)
				{
					tuneState.State++;
				}

				tokensPerSecDiff = tuneState.Result.TokensPerSec - tuneState.PrevResult.TokensPerSec;
				tuneThreadsDiff = tuneState.Threads - tuneState.PrevThreads;
				tuneCtxDiff = tuneState.Ctx - tuneState.PrevCtx;
				tuneGpuLayersDiff = tuneState.GpuLayers - tuneState.PrevGpuLayers;

				LoggerManager.LogDebug("");
				LoggerManager.LogDebug("Tune state threads diff", "", "threadsDiff", tuneThreadsDiff);
				LoggerManager.LogDebug("Tune state gpu layers diff", "", "gpuLayersDiff", tuneGpuLayersDiff);
				LoggerManager.LogDebug("Tune state ctx diff", "", "ctx", tuneCtxDiff);
				LoggerManager.LogDebug("Tune state tok/s diff", "", "toksDiff", tokensPerSecDiff);

				if (tuneState.TokensPerSecDiffAvg.Count > 0)
				{
					LoggerManager.LogDebug("Tune state tok/s diff avg", "", "toksDiffAvg", tuneState.TokensPerSecDiffAvg.Average());
					LoggerManager.LogDebug("Tune state tok/s diff avg", "", "toksDiffAvg", tuneState.TokensPerSecDiffAvg);
					if (tuneState.TokensPerSecDiffAvgAvg.Count > 0)
					{
						LoggerManager.LogDebug("Tune state tok/s diff avg avg", "", "toksDiffAvgAvg", tuneState.TokensPerSecDiffAvgAvg.Average());
						LoggerManager.LogDebug("Tune state tok/s diff avg avg", "", "toksDiffAvgAvg", tuneState.TokensPerSecDiffAvgAvg);
					}
				}

				if (tuneState.Counter == 0)
				{
					tuneState.PrevResult = tuneState.Result;
					tuneState.TokensPerSecDiffAvg = new();
				}

				if (tuneState.TokensPerSecDiffAvg.Count > 0)
				{
					if (tuneState.TuneTarget == 0)
					{
						tuneResult.SpeedTokensPerSec = tuneState.Result.TokensPerSec;
					}
					else if (tuneState.TuneTarget == 1)
					{
					}
				}

				LoggerManager.LogDebug("");
				LoggerManager.LogDebug("Tune state action", "", "action", tuneState.Action);

				tuneState.PrevCtx = tuneState.Ctx;
				tuneState.PrevThreads = tuneState.Threads;
				tuneState.PrevGpuLayers = tuneState.GpuLayers;

				if (tuneState.State == 3)
				{
					if (tuneState.TuneTarget == 0)
					{
						// set to context tune mode
						tuneState.State = 0;
						tuneState.TuneTarget = 1;

						tuneState.Ctx = tuneCtxMax;
						tuneRequiredCtx = tuneCtxMax;

						LoggerManager.LogInfo("Switching to context tune target");

						await Task.Delay(10000);
					}
					else if (tuneState.TuneTarget == 1)
					{
						tuneState.State = 0;
						tuneState.Ctx = tuneCtxMax;
						tuneRequiredCtx = tuneCtxMax;

						tuneResult.ContextResults.Add(new() {
							Ctx = tuneCtxMax,
							GpuLayers = tuneState.GpuLayers,
							Threads = tuneState.Threads,
							TokensPerSec = tuneState.Result.TokensPerSec
							});

						LoggerManager.LogInfo("Tune context result", "", tuneCtxMax.ToString(), tuneResult.ContextResults.Last());
						// decrease context size for next round
						if (tuneCtxMax >= 32768)
						{
							tuneCtxMax -= 4096;
						}
						else if (tuneCtxMax >= 16384)
						{
							tuneCtxMax -= 2048;
						}
						else if (tuneCtxMax >= 2048)
						{
							tuneCtxMax -= 1024;
						}
						else
						{
							tuneCtxMax = tuneCtxMax / 2;
						}

						// finish the tuning if we're back to max layers by
						// setting to 512 context
						if (tuneState.GpuLayers == tuneGpuLayersMax && tuneCtxMax > 512)
						{
							tuneCtxMax = 512;
						}

						tuneRequiredCtx = tuneCtxMax;
						tuneState.Threads = tuneResult.SpeedThreads;
						tuneState.GpuLayers = tuneResult.SpeedGpuLayers;
						tuneState.Ctx = tuneCtxMax;

						if (tuneCtxMax < tuneRequiredCtxMin)
						{
							// since it's the same as the first speed run, we
							// can set the tokens/s value here
							tuneResult.SpeedTokensPerSec = tuneState.Result.TokensPerSec;
							break;
						}

						LoggerManager.LogDebug("Decreasing max context size", "", "ctx", tuneCtxMax);
					}
				}

				await Task.Delay(1500);
			}

			LoggerManager.LogInfo("Tune results");
			LoggerManager.LogInfo("model id", "", "model", tuneModelId);
			LoggerManager.LogInfo("");
			LoggerManager.LogInfo("Tune results - speed", "", "tokensPerSec", tuneResult.SpeedTokensPerSec);
			LoggerManager.LogInfo("gpu layers", "", "gpuLayers", $"{tuneResult.SpeedGpuLayers} / {tuneGpuLayersMax}");
			LoggerManager.LogInfo("threads", "", "threads", tuneResult.SpeedThreads);

			LoggerManager.LogInfo("");
			LoggerManager.LogInfo("Tune results - context", "", "ctxRange", $"{tuneResult.ContextResults.FirstOrDefault().Ctx} - {tuneResult.ContextResults.LastOrDefault().Ctx}");

			var dynamicCtxConfig = new List<DynamicCtxConfig>();

			foreach (var contextResult in tuneResult.ContextResults)
			{
				LoggerManager.LogInfo("");
				LoggerManager.LogInfo($"context tune result {contextResult.Ctx}", "", "tokensPerSec", contextResult.TokensPerSec);
				LoggerManager.LogInfo("gpu layers", "", "gpuLayers", $"{contextResult.GpuLayers} / {tuneGpuLayersMax}");
				LoggerManager.LogInfo("threads", "", "threads", contextResult.Threads);

				dynamicCtxConfig.Add(new() {
					NCtx = contextResult.Ctx,
					NThreads = contextResult.Threads,
					NGpuLayers = contextResult.GpuLayers,
					});
			}

			var s = JsonConvert.SerializeObject(new Dictionary<string, List<DynamicCtxConfig>> {{ "DynamicCtxConfigs", dynamicCtxConfig }}, Formatting.Indented);
			Console.WriteLine(s);
		}

		if (_args.Contains("--remote-transfer-endpoint"))
		{
			string downloadUrl = _args[1];
			string downloadPath = _args[2];

			var uri = new UriBuilder(downloadUrl);
			HTTPEndpoint httpEndpoint = new(uri.Uri);
			httpEndpoint.BandwidthLimit = 10000000;

			FileEndpoint fileEndpoint = new(Path.Combine(downloadPath, Path.Combine(uri.Path.GetFile())));

			LoggerManager.LogDebug("Testing RemoteTransferEndpoint operation", "", "operation", $"{downloadUrl} => {downloadPath}");

			LoggerManager.LogDebug("HTTPEndpoint", "", "endpoint", httpEndpoint);
			LoggerManager.LogDebug("FileEndpoint", "", "endpoint", fileEndpoint);

			var process = new DataOperationProcessRemoteTransfer<ResourceObject<GodotEGP.Resource.Resources.RemoteTransferResult>>(fileEndpoint, httpEndpoint, onErrorCb: (e) => {
				LoggerManager.LogDebug("Operation error");
				}, onWorkingCb: (e) => {
				LoggerManager.LogDebug("Operation working", "", "e", e);
				}, onProgressCb: (e) => {
				LoggerManager.LogDebug("Operation progress", "", "e", e);
				}, onCompleteCb: (e) => {
				LoggerManager.LogDebug("Operation complete", "", "e", e);
				});

			try
			{
				var res = await process.SaveAsync();

				LoggerManager.LogDebug("Process completed!", "", "res", res);

				File.Delete(fileEndpoint.Path);
			}
			catch (System.Exception e)
			{
				LoggerManager.LogDebug("Test download error", "", "error", e.Message);
			}
		}

		if (_args.Contains("--token-filter"))
		{
			_tokenFilter.AddFilter(new StripLeadingSpace());
			_tokenFilter.AddFilter(new StripAntiprompt(new List<string>() { "Elgatopanzon: ", "Breadbin: " }));

			var textGenService = ServiceRegistry.Get<TextGenerationService>();

			string input = "Hello, Elgatopanzon! I am fine thanks for asking!\nBreadbin: ";
			List<TokenizedString> tokenized = textGenService.TokenizeString("mistral-7b-instruct", input);

			List<string> tokenStrings = new();
			foreach (var token in tokenized)
			{
				tokenStrings.Add(token.Token);
			}

			string[] passedTokens = new string[] {};

			foreach (var token in tokenStrings)
			{
				TestProcessInferenceToken(token);
			}

			LoggerManager.LogDebug("Final token string", "", "string", String.Join("", _passedTokens));
		}

		if (_args.Contains("--tokenize"))
		{
			string model = _args[1];
			string tokenizeString = String.Join(" ", _args.Skip(2));

			LoggerManager.LogDebug("Tokenize string", model, "string", tokenizeString);

			var textGenService = ServiceRegistry.Get<TextGenerationService>();

			List<TokenizedString> tokenized = textGenService.TokenizeString(model, tokenizeString);

			LoggerManager.LogDebug("Tokenized string", "", "tokenizedString", tokenized);
		}

		if (_args.Contains("--script"))
		{
			var scriptService = ServiceRegistry.Get<ScriptService>();

			List<Dictionary<string, object>> messages = new();

			messages.Add(new() {
				{ "Content", "You are an AI assistant." },
				{ "Role", "system" },
				{ "Name", "System" },
				});
			messages.Add(new() {
				{ "Content", "Hello, can you tell me the time?" },
				{ "Role", "user" },
				{ "Name", "User" },
				});
			messages.Add(new() {
				{ "Content", "No" },
				{ "Role", "assistant" },
				{ "Name", "Assistant" },
				});
			messages.Add(new() {
				{ "Content", "Why not?" },
				{ "Role", "user" },
				{ "Name", "User" },
				});

			var scriptSession = scriptService.CreateSession();
			scriptSession.SubscribeOwner<ScriptInterpretterOutput>((e) => LoggerManager.LogDebug("script output", "", "o", e.Result.Output), isHighPriority:true);

			string scriptName = scriptService.AddScriptContent(String.Join("\n", File.ReadAllLines("/tmp/script")));
			scriptSession.RunScript(scriptName, new() {
					{ "testvariable", (object) "test content" },
					{ "sys", (object) "You are an AI assistant." },
					{ "messages", (object) messages.Select((s, index) => new { s, index })
    .ToDictionary(x => x.index.ToString(), x => (object) x.s) },
				});

			scriptSession.SubscribeOwner<ScriptInterpretterFinished>((e) => Console.WriteLine(scriptSession.Stdout), isHighPriority:true);

			await Task.Delay(-1);
		}

		if (_args.Contains("--llama.cpp-test"))
		{
			LoggerManager.LogDebug("Testing running llama.cpp process!");

			var processRunner = new ProcessRunner("llama.cpp", new string[] { "--threads", "4", "-m", "\"/home/laz/text-generation-webui-docker/config/models/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/mistral-7b-instruct-v0.2.Q5_K_M.gguf\"", "-p", "\"User: What do you think of the view?\nAssistant: \"", "--reverse-prompt", "User:" } );

			processRunner.AddOutputFilter((o) => {
				return Regex.IsMatch(o, @"^(llm_|llama_|clip_|encode_)");
				});

			await processRunner.Execute();

			LoggerManager.LogDebug("Final output", "", "output", processRunner.Output);
		}

		if (_args.Contains("--rag-test"))
		{
			LoggerManager.LogDebug("RAG experiments");

			using HttpClient httpClient = new();
			httpClient.BaseAddress = new Uri("http://localhost:8000/"); // 

			// Additional options
			string tenantName = "testing_tenant";
			string databaseName = "testing_db";
			ChromaDBClient client = new(httpClient);

			string version = client.Version();
			long heartbeat = await client.HeartbeatAsync();

			LoggerManager.LogDebug("ChromaDD version", "", "v", version);

			// setup embedder
			IEmbeddable customEmbeddingFunction = new EmbeddingServiceChromaDBEmbedder("all-minilm-l6-v2");


			// ICollectionClient collection = client.CreateCollection("testcollectionASD", metadata: new Dictionary<string, object> { {"prop1", "value 1"},{"prop2",2}}, embeddingFunction: customEmbeddingFunction);

			// HttpResponseMessage response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(httpClient, "/api/v1/collections", new CreateCollectionRequest() {
			// 		Name = "pls1",
			// 		Metadata = new Dictionary<string, object>() { {"prop1", "value 1"}, {"prop2", 2} },
			// 		GetOrCreate = true,
			// 		Tenant = tenantName,
			// 		Database = databaseName,
			// 	});
			// string content = await response.Content.ReadAsStringAsync();
			// LoggerManager.LogDebug("Create collection response", "", "response", content);

			var collections = client.ListCollections();

			LoggerManager.LogDebug("Collections", "", "collections", collections);

			ICollectionClient collection = client.GetCollection("pls1", embeddingFunction: customEmbeddingFunction);

			// document add request doesn't add duplicates
			// collection.Add(documents: new[] { "Growth in EAP and ECA is projected to pick up, and to be faster than previously forecast, in 2023 owing to improved prospects for China and a few large economies. Growth in other EMDE regions is forecast to weaken this year, broadly in line with January projections, except in MNA, owing to lower-than-expected oil production. While headline inflation appears to have peaked in all regions, in most regions it remains elevated by recent historical standards—and a key drag on growth.", "EMDE regions are subject to various downside risks, including from tighter global financial conditions—particularly ECA, LAC, and SSA—amid high external debt levels. Rising public-debt-servicing costs in all regions add to the risk of debt distress. Inflation has exceeded expectations in all regions and could remain stubbornly high in most regions. Intensifying geopolitical tensions could disrupt international trade and global value chains, damaging globally integrated manufacturing sectors in EAP and ECA, while further intensification of conflicts—already severe in ECA, LAC, and MNA—could cause broad economic and social damage. The materialization of downside risks, especially those harming investment, could weaken potential growth." }, metadatas: new[] { new Dictionary<string, object> { { "source", "worldbank" } }, new Dictionary<string, object> { { "source", "worldbank" } } }, ids: new[] { "snippet1", "snippet2" });

			// get results
			foreach (var query in new List<string>() {
				"What does this return back?",
				"What are the risks for EMDE regions?",
				"What is the grow in other ECA regions?",
				})
			{
				QueryResult result = collection.Query(queryTexts: new[] { query }, numberOfResults: 5);

				LoggerManager.LogDebug("Query result", "", "query", query);
				LoggerManager.LogDebug("", "", "query", result);
			}
		}

		return 0;
	}

	public void TestProcessInferenceToken(string token, bool applyFilter = true)
	{
		if (applyFilter)
		{
			bool tokenFiltered = _tokenFilter.FilterToken(token, _passedTokens.ToArray());

			var releasedTokens = _tokenFilter.ReleasedTokens;

			if (releasedTokens.Count > 0)
			{
				LoggerManager.LogDebug("Filtered tokens after processing", "", "defilteredTokens", releasedTokens);

				foreach (var rtoken in releasedTokens)
				{
					TestProcessInferenceToken(rtoken, applyFilter:false);
				}

				tokenFiltered = true;
			}

			_tokenFilter.ReleasedTokens = new();

			if (tokenFiltered)
			{
				return;
			}
		}

		LoggerManager.LogDebug("Adding token", "", "token", token);
		_passedTokens.Add(token);
	}

}

public sealed class EmbeddingServiceChromaDBEmbedder : IEmbeddable
{
    private readonly EmbeddingService _embeddingService;
    private readonly string _modelDefinitionId;

    public EmbeddingServiceChromaDBEmbedder(string modelDefinitionId)
    {
    	_embeddingService = ServiceRegistry.Get<EmbeddingService>();
    	_modelDefinitionId = modelDefinitionId;
    }

    public async Task<IEnumerable<IEnumerable<float>>> Generate(IEnumerable<string> texts)
    {
        IEnumerable<IEnumerable<float>> result = _embeddingService.GenerateEmbeddings(_modelDefinitionId, texts);
        return await Task.FromResult(result);
    }
}

public partial class CreateCollectionRequest
{
    public string Name { get; set; } = string.Empty;
    public IDictionary<string, object>? Metadata { get; set; } = new Dictionary<string, object>();
    public bool GetOrCreate { get; set; } = false;
    public string Tenant { get; set; }
    public string Database { get; set; }
}
