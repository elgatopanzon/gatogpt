/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionsController
 * @created     : Friday Jan 05, 2024 13:00:42 CST
 */

namespace GatoGPT.WebAPI.v1.Controllers;

using GatoGPT.Service;
using GatoGPT.Config;
using GatoGPT.LLM;
using GatoGPT.WebAPI.Dtos;
using GatoGPT.WebAPI.Entities;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using AutoMapper;
// using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
// using GatoGPT.WebAPI.Dtos;
// using GatoGPT.WebAPI.Entities;
using System.Text.Json;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public partial class ChatController : ControllerBase
{
	private readonly IMapper _mapper;
	private readonly LlamaModelManager _modelManager;
	private readonly LlamaInferenceService _inferenceService;

	public ChatController(IMapper mapper)
	{
		_mapper = mapper;
		 _modelManager = ServiceRegistry.Get<LlamaModelManager>();
		 _inferenceService = ServiceRegistry.Get<LlamaInferenceService>();
	}

    [HttpPost("completions", Name = nameof(CreateChatCompletion))]
    public async Task<ActionResult<ChatCompletionDto>> CreateChatCompletion(ApiVersion version, [FromBody] ChatCompletionCreateDto chatCompletionCreateDto)
    {
    	LoggerManager.LogDebug("Recieved chatCompletionCreateDto", "", "create", chatCompletionCreateDto);

    	// validate required params
		if (chatCompletionCreateDto.Model.Length == 0)
		{
			return BadRequest(new InvalidRequestErrorDto(
						message: "You must provide a model parameter",
						code: null,
						param:null
						));
		}

		// check model is valid
		if (!_modelManager.ModelDefinitions.ContainsKey(chatCompletionCreateDto.Model))
		{
    		return NotFound(new InvalidRequestErrorDto(message:$"The model '{chatCompletionCreateDto.Model}' does not exist", code:"model_not_found", param:"model"));
		}

		if (chatCompletionCreateDto.Messages.Count == 0)
		{
			return BadRequest(new InvalidRequestErrorDto(
						message: "You must provide a messages parameter",
						code: null,
						param:null
						));
		}

		// extract stops
		List<string> stops = new();
		if (chatCompletionCreateDto.Stop is System.String stopString)
		{
			stops.Add(stopString);
		}
		else if (chatCompletionCreateDto.Stop is Newtonsoft.Json.Linq.JArray stopsList)
		{
			stops = stopsList.ToArray().Select(x => x.ToString()).ToList();
		}

		LoggerManager.LogDebug("Completion dto extracted stops", "", "stops", stops);

		// create LoadParams and InferenceParams objects from dto
		LLM.LoadParams loadParams = _modelManager.GetModelDefinition(chatCompletionCreateDto.Model).ModelProfile.LoadParams.DeepCopy();
		LLM.InferenceParams inferenceParams = _modelManager.GetModelDefinition(chatCompletionCreateDto.Model).ModelProfile.InferenceParams.DeepCopy();

		var completionCreateDtoDefault = new CompletionCreateDto();

		if (chatCompletionCreateDto.Temperature != completionCreateDtoDefault.Temperature)
			inferenceParams.Temp = chatCompletionCreateDto.Temperature;
		if (chatCompletionCreateDto.FrequencyPenalty != completionCreateDtoDefault.FrequencyPenalty)
			inferenceParams.FrequencyPenalty = chatCompletionCreateDto.FrequencyPenalty;
		if (chatCompletionCreateDto.PresencePenalty != completionCreateDtoDefault.PresencePenalty)
			inferenceParams.PresencePenalty = chatCompletionCreateDto.PresencePenalty;
		if (chatCompletionCreateDto.RepeatPenalty != completionCreateDtoDefault.RepeatPenalty)
			inferenceParams.RepeatPenalty = chatCompletionCreateDto.RepeatPenalty;
		if (chatCompletionCreateDto.MaxTokens != completionCreateDtoDefault.MaxTokens)
			inferenceParams.NPredict = chatCompletionCreateDto.MaxTokens;
		if (chatCompletionCreateDto.Seed != completionCreateDtoDefault.Seed)
			loadParams.Seed = chatCompletionCreateDto.Seed;

		inferenceParams.Antiprompts = inferenceParams.Antiprompts.Concat(stops).ToList();

		if (chatCompletionCreateDto.MinP != completionCreateDtoDefault.MinP)
			inferenceParams.MinP = chatCompletionCreateDto.MinP;
		if (chatCompletionCreateDto.TopP != completionCreateDtoDefault.TopP)
			inferenceParams.TopP = chatCompletionCreateDto.TopP;
		if (chatCompletionCreateDto.TopK != completionCreateDtoDefault.TopK)
			inferenceParams.TopK = chatCompletionCreateDto.TopK;

		// init new chat instance
    	StatefulChat chatInstance = new(false, loadParams, inferenceParams);
    	List<StatefulChatMessage> messageEntities = new();

    	// testMessages.Add(new() {
		// 	Role = "system",
		// 	Content = "You are a helpful assistant."
    	// 	});
    	// testMessages.Add(new() {
		// 	Role = "user",
		// 	Content = "Hello!"
    	// 	});
    	// testMessages.Add(new() {
		// 	Role = "assistant",
		// 	Content = "Hi, how can I help you?"
    	// 	});
    	// testMessages.Add(new() {
		// 	Role = "user",
		// 	Content = "I don't know, can you tell me how?"
    	// 	});

    	foreach (var messageCreateDto in chatCompletionCreateDto.Messages)
    	{
    		messageEntities.Add(new StatefulChatMessage() {
				Content = messageCreateDto.Content,
				Role = messageCreateDto.Role,
				Name = messageCreateDto.Name,
    			});
    	}

    	chatInstance.SetChatMessages(messageEntities);

    	LoggerManager.LogDebug("Prompt to send", "", "prompt", chatInstance.GetPrompt());

		ChatCompletionDto chatCompletionDto = new();

		// queue and generate responses until N is reached
		int currentIndex = 0;
		while (chatCompletionDto.Choices.Count < chatCompletionCreateDto.N)
		{
    		LlamaModelInstance modelInstance = _inferenceService.CreateModelInstance(chatCompletionCreateDto.Model, stateful:true);
			StatefulChatMessage message = await chatInstance.ChatAsync(modelInstance);

			chatCompletionDto.Usage.PromptTokens += modelInstance.InferenceResult.PromptTokenCount;
			chatCompletionDto.Usage.CompletionTokens += modelInstance.InferenceResult.GenerationTokenCount;

			ChatCompletionMessageDto messageDto = new() {
				Content = message.Content,
				Role = message.Role,
			};

			chatCompletionDto.Choices.Add(new ChatCompletionChoiceDto() {
				FinishReason = (modelInstance.InferenceResult.Tokens.Count >= chatCompletionCreateDto.MaxTokens ? "length" : "stop"),
				Index = currentIndex,
				InferenceResult = modelInstance.InferenceResult,
				Message = messageDto,
				});

			currentIndex++;
		}

		chatCompletionDto.Id = $"cmpl-{GetHashCode()}-{chatCompletionDto.GetHashCode()}-{chatCompletionCreateDto.GetHashCode()}";
		chatCompletionDto.Created = ((DateTimeOffset) DateTime.UtcNow).ToUnixTimeSeconds();
		chatCompletionDto.Model = chatCompletionCreateDto.Model;
		chatCompletionDto.SystemFingerprint = GetHashCode().ToString();

    	return Ok(chatCompletionDto);
    }
}

