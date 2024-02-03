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
using GodotEGP.Data;
using GodotEGP.Data.Endpoint;
using GodotEGP.Data.Operation;
using GodotEGP.Data.Operator;
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

	public async Task<int> Run()
	{
		LoggerManager.LogDebug("Testing class!");

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

			var process = new DataOperationProcessRemoteTransfer<Resource<GodotEGP.Resource.RemoteTransferResult>>(fileEndpoint, httpEndpoint, onErrorCb: (e) => {
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
