/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : OpenAI
 * @created     : Sunday Jan 14, 2024 18:36:16 CST
 */

namespace GatoGPT.AI.OpenAI;

using GatoGPT.Config;
using GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public partial class OpenAI
{
	public OpenAIConfig Config { get; set; }
	public ErrorResult Error  { get; set; }

	public OpenAI(OpenAIConfig config)
	{
		Config = config;
	}

	public async Task<string> MakeRequestPost(string endpoint = "", object requestObj = null, bool useSse = false)
	{
		LoggerManager.LogDebug("Making request", "", "requestObj", requestObj);

		var httpClient = new HttpClient();

		var jsonContent = JsonConvert.SerializeObject(requestObj, 
        				new JsonSerializerSettings
					{
    					ContractResolver = new DefaultContractResolver() { NamingStrategy = new SnakeCaseNamingStrategy() },
					});

		// serialise object to snake case json
		var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
		httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Config.APIKey);

		LoggerManager.LogDebug("Making request body", "", "requestJson", jsonContent);

		// send the request to the server and watch for server sent events if
		// stream: true
		string responseString = "";

		using (var request = new HttpRequestMessage(HttpMethod.Post, Config.Host+endpoint){ 
				Content = content
				})
		using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))

		if (useSse)
		{
			using (var theStream = await response.Content.ReadAsStreamAsync())
			using (var theStreamReader = new StreamReader(theStream))
			{
    			string sseLine = null;

    			while ((sseLine = await theStreamReader.ReadLineAsync()) != null)
    			{
    				// hackily parse server sent events
    				if (sseLine.StartsWith("data: "))
    				{
    					LoggerManager.LogDebug("SSE event received", "", "sseEvent", sseLine);

    					this.Emit<OpenAIServerSentEvent>(e => e.Event = sseLine.Replace("data: ", ""));
    				}
    			}
			};

			httpClient.CancelPendingRequests();
			request.Dispose();
			response.Dispose();
			httpClient.Dispose();
		}
		else
		{
			var contents = await response.Content.ReadAsStringAsync();
			request.Dispose();
			response.Dispose();
			httpClient.Dispose();

			var errorResult = GetResultObject<ErrorResult>(contents);

			if (errorResult.Error != null)
			{
				Error = errorResult;

				LoggerManager.LogDebug("Request error response", "", "error", Error);

				return "";
			}

			responseString = contents;

		}
		return responseString;
	}

	public async Task<string> MakeRequestGet(string endpoint = "")
	{
		string contents = "";
		using (var client = new HttpClient(new HttpClientHandler {  }))
        {
            client.BaseAddress = new Uri(Config.Host);
            HttpResponseMessage response = client.GetAsync(endpoint).Result;
            contents = response.Content.ReadAsStringAsync().Result;
        }

		var errorResult = GetResultObject<ErrorResult>(contents);

		if (errorResult.Error != null)
		{
			Error = errorResult;

			LoggerManager.LogDebug("Request error response", "", "error", Error);

			return "";
		}

		return contents;
	}

	public T GetResultObject<T>(string jsonObj)
	{
    	var resultObj = JsonConvert.DeserializeObject<T>(jsonObj, new JsonSerializerSettings {
    		ContractResolver = new DefaultContractResolver() { NamingStrategy = new SnakeCaseNamingStrategy() }}
		);

		return resultObj;
	}

	// /v1/embeddings
	public async Task<EmbeddingsDto> Embeddings(EmbeddingCreateDto request)
	{
		return GetResultObject<EmbeddingsDto>(await MakeRequestPost("/v1/embeddings", request, false));
	}

	// /v1/completions
	public async Task<CompletionDto> Completions(CompletionCreateOpenAIDto request)
	{
		return GetResultObject<CompletionDto>(await MakeRequestPost("/v1/completions", request, request.Stream));
	}
	
	// /v1/chat/completions
	public async Task<ChatCompletionDto> ChatCompletions(ChatCompletionCreateOpenAIDto request)
	{
		return GetResultObject<ChatCompletionDto>(await MakeRequestPost("/v1/chat/completions", request, request.Stream));
	}

	// /v1/models
	public async Task<ModelListDto> Models()
	{
		return GetResultObject<ModelListDto>(await MakeRequestGet("/v1/models"));
	}

	// /v1/models/{model}
	public async Task<ModelDto> Models(string model)
	{
		return GetResultObject<ModelDto>(await MakeRequestGet($"/v1/models/{model}"));
	}
}

public partial class OpenAIEvent : Event {}
public partial class OpenAIServerSentEvent : OpenAIEvent {
	public string Event { get; set; }
}
