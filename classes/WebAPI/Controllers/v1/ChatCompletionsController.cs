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
using System.Text.RegularExpressions;
using Newtonsoft.Json;

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

		// setup tools
		string toolChoice = chatCompletionCreateDto.GetToolChoice();
		ChatCompletionToolChoiceDto toolChoiceDto = null;

		// if tool choice is empty, then it contains a function to override
		if (toolChoice == "")
		{
			toolChoiceDto = chatCompletionCreateDto.GetToolChoiceObject();

			LoggerManager.LogDebug("Overriding tool choice", "", "toolChoice", toolChoiceDto);

			// TODO: figured out something nicer?
			toolChoice = toolChoiceDto.Function.Name;
		}
		// automatically choose between listed tools
		else if (toolChoice == "auto")
		{
			LoggerManager.LogDebug("Tool choice set to auto");
		}
		else if (toolChoice == "none")
		{
			LoggerManager.LogDebug("Tool choice set to none");
		}

		// inject a list of tools into the system prompt
		List<string> validFunctionNames = new();
		if (toolChoice != "none" && chatCompletionCreateDto.Tools.Count > 0)
		{
			var lastMessage = chatCompletionCreateDto.Messages.Last();

			string messageOriginalContent = lastMessage.Content;
			lastMessage.Content = "";

			lastMessage.Content += $"As an AI assistant you have access to a range of tools. Please select the most suitable function and parameters from the list of available functions below. Provide your response in JSON format when calling a function, otherwise respond as a human would.";
			lastMessage.Content += $"\nAvailable functions:";

			foreach (var tool in chatCompletionCreateDto.Tools)
			{
				validFunctionNames.Add(tool.Function.Name);

				lastMessage.Content += $"\n{tool.Function.Name}";

				if (tool.Function.Description.Length > 0)
				{
					lastMessage.Content += $" ({tool.Function.Description})";
				}

				LoggerManager.LogDebug("Params parsed", "", "params", tool.Function.GetParametersDto());

				var functionParams = tool.Function.GetParametersDto();

				lastMessage.Content += $"\n  Parameters:";

				foreach (var param in functionParams.Properties)
				{
					lastMessage.Content += $"\n    {param.Key}:";

					lastMessage.Content += $"\n      data_type: {param.Value.Type}";

					if (param.Value.Description.Length > 0)
					{
						lastMessage.Content += $"\n      description: {param.Value.Description}";
					}

					if (param.Value.Enum.Count > 0)
					{
						lastMessage.Content += $"\n      allowed_values: {String.Join(", ", param.Value.Enum)}";
					}

					lastMessage.Content += $"\n      required: {(functionParams.Required.Contains(param.Key)).ToString()}";
					
				}

				lastMessage.Content += "\n  Example responses:";
				lastMessage.Content += "\n  { \"function\": \""+tool.Function.Name+"\", \"arguments\": {\"argument_name\": \"argument_value\"}}";


				foreach (var includeAllParams in new List<bool>() { true, false })
				{
					lastMessage.Content += "\n  { \"function\": \""+tool.Function.Name+"\"";

					if (functionParams.Properties.Count > 0)
					{
						lastMessage.Content += "{ \"arguments\": ";

						foreach (var param in functionParams.Properties)
						{
							if (!includeAllParams && !functionParams.Required.Contains(param.Key))
							{
								continue;
							}
							string functionExampleValue = "ARGUMENT_VALUE";

							if (param.Value.Enum.Count > 0)
							{
								functionExampleValue = $"e.g. {String.Join(", ", param.Value.Enum)}";
							}

							lastMessage.Content += "{\""+param.Key+"\": \""+functionExampleValue+"\"},";
						}

						lastMessage.Content = lastMessage.Content.TrimEnd(',');

						lastMessage.Content += "}";
					}

					lastMessage.Content += "}";
				}
				lastMessage.Content += $"\n";
			}

			if (toolChoice == "auto")
			{
				lastMessage.Content += $"\nYou can choose to ignore the above functions and respond normally to the user's request if the request has nothing related to your available functions.";
			}

			inferenceParams.PrePrompt = lastMessage.Content;
			lastMessage.Content = messageOriginalContent;
		}

		LoggerManager.LogDebug("Available tools", "", "tools", chatCompletionCreateDto.Tools);

		// init new chat instance
    	StatefulChat chatInstance = new(false, loadParams, inferenceParams);
    	List<StatefulChatMessage> messageEntities = new();

    	foreach (var messageCreateDto in chatCompletionCreateDto.Messages)
    	{
    		messageEntities.Add(new StatefulChatMessage() {
				Content = messageCreateDto.Content,
				Role = messageCreateDto.Role,
				Name = messageCreateDto.Name,
				ToolCalls = messageCreateDto.ToolCalls,
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

			string finishReason = (modelInstance.InferenceResult.Tokens.Count >= chatCompletionCreateDto.MaxTokens ? "length" : "stop");

			// parse tool calls
			string toolParseResult = "";
			if (toolChoice != "none" && chatCompletionCreateDto.Tools.Count > 0)
			{
				// strip useless text
				// string toolCallContent = ;
				var match = Regex.Match(message.Content, @"(?<json>{(?:[^{}]|(?<Nested>{)|(?<-Nested>}))*(?(Nested)(?!))})");

				while (match.Success)
				{
					if (match.Groups.Count > 0 && match.Groups[0].Value.Length > 0)
					{
						LoggerManager.LogDebug("JSON tool parse", "", "toolParse", match.Groups[0].Value);
						messageDto.Content = match.Groups[0].Value.Trim();

						finishReason = "tool_call";
					}

					match = match.NextMatch();
				}

				// parse into toolcall dto
				if (finishReason == "tool_call")
				{
					try
					{
						var toolCallRawDto = JsonConvert.DeserializeObject<ChatCompletionToolCallRawDto>(messageDto.Content);;

						var toolCallDto = new ChatCompletionToolCallDto();
						toolCallDto.Type = "function";
						toolCallDto.Id = $"toolcall-{GetHashCode()}-{toolCallDto.GetHashCode()}";

						LoggerManager.LogDebug("Parsed toolCallRawDto", "", "toolCallRawDto", toolCallRawDto);

						toolCallDto.Function = new ChatCompletionToolFunctionDto() {
							Name = toolCallRawDto.Function,
							Arguments = JsonConvert.SerializeObject(toolCallRawDto.Arguments),
						};

						LoggerManager.LogDebug("Final toolCallDto", "", "toolCallDto", toolCallDto);

						if (toolCallDto.Function.Name == null || !validFunctionNames.Contains(toolCallDto.Function.Name))
						{
							finishReason = "";

							toolParseResult = "Not a valid function name";
						}
						else
						{
							messageDto.ToolCalls.Add(toolCallDto);
						}

					}
					catch (System.Exception)
					{
						LoggerManager.LogDebug("Parsing toolCallDto failed", "", "messageContent", messageDto.Content);
						finishReason = "";

						toolParseResult = "Not a valid JSON response";
					}
				}

				if (finishReason != "tool_call")
				{
					LoggerManager.LogDebug("Result not valid JSON");
					modelInstance.InferenceParams.Temp -= Math.Max(0, 0.1);

    				messageEntities.Add(new StatefulChatMessage() {
						Content = messageDto.Content,
						Role = "assistant",
    					});
    				messageEntities.Add(new StatefulChatMessage() {
						Content = $"The function call was invalid. Reason: {toolParseResult}",
						Role = "user",
    					});

    				chatInstance.SetChatMessages(messageEntities);

					continue;
				}
			}

			chatCompletionDto.Choices.Add(new ChatCompletionChoiceDto() {
				FinishReason = finishReason,
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

